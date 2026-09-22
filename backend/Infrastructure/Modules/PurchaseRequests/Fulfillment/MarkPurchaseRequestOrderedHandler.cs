using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestOrdered;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Fulfillment;

/// <summary>Applies the Approved to Ordered transition for Procurement.</summary>
public sealed class MarkPurchaseRequestOrderedHandler : IMarkPurchaseRequestOrderedHandler
{
    private const string ConcurrencyConflictMessage =
        "Purchase request was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestFulfillmentStore _store;

    public MarkPurchaseRequestOrderedHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestFulfillmentStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        MarkPurchaseRequestOrderedCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Fulfillment identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Purchase request concurrency stamp is required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        if (!PurchaseRequestFulfillmentAccess.CanManage(membership))
        {
            return Failure(
                PurchaseRequestFulfillmentAccess.DeniedStatus(membership),
                "The current user cannot manage purchase request fulfillment.");
        }

        var request = await _store.GetRequestAsync(
            membership,
            command.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "An approved or ordered purchase request was not found.");
        }

        if (!HasExpectedVersion(request.ConcurrencyStamp, command.ExpectedConcurrencyStamp))
        {
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                ConcurrencyConflictMessage);
        }

        try
        {
            var previousStatus = request.Status;
            var now = DateTime.UtcNow;
            request.MarkOrdered(command.OrderNumber, command.FulfillmentNote);
            _store.AddStatusHistory(PurchaseRequestStatusHistory.Create(
                request.Id,
                previousStatus,
                request.Status,
                command.UserId,
                now));
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.ValidationError, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _store.ClearChangeTracker();
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                ConcurrencyConflictMessage);
        }
        catch (DbUpdateException)
        {
            _store.ClearChangeTracker();
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                "Purchase request fulfillment could not be saved.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
            PurchaseRequestViewMapper.ToDetailsView(request),
            "Purchase request marked as ordered.");
    }

    private static bool HasExpectedVersion(string current, string expected)
        => string.Equals(current, expected.Trim(), StringComparison.Ordinal);

    private static PurchaseRequestOperationResult<PurchaseRequestDetailsView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(status, message);
}
