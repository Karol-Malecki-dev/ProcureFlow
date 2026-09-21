using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.CancelPurchaseRequest;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.CancelPurchaseRequest;

/// <summary>
/// Coordinates author access, aggregate cancellation and atomic status-history persistence.
/// </summary>
public sealed class CancelPurchaseRequestHandler : ICancelPurchaseRequestHandler
{
    private const string ConcurrencyConflictMessage = "Purchase request was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestWorkflowStore _workflowStore;

    public CancelPurchaseRequestHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestWorkflowStore workflowStore)
    {
        _membershipReader = membershipReader;
        _workflowStore = workflowStore;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        CancelPurchaseRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request concurrency stamp is required.");
        }

        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        if (!membership.IsActiveEmployeeScope)
        {
            return Failure(
                membership.Role == Domain.Models.Organizations.Enums.BusinessRole.Employee
                    ? PurchaseRequestOperationStatus.Conflict
                    : PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot cancel purchase requests in this scope.");
        }

        var request = await _workflowStore.GetOwnedAsync(
            membership,
            command.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Purchase request was not found.");
        }

        if (!HasExpectedVersion(request.ConcurrencyStamp, command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }

        if (request.Status is not (PurchaseRequestStatus.Draft or PurchaseRequestStatus.Submitted))
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, "Only draft or submitted purchase requests can be cancelled.");
        }

        try
        {
            var previousStatus = request.Status;
            request.Cancel();
            _workflowStore.AddStatusHistory(PurchaseRequestStatusHistory.Create(
                request.Id,
                previousStatus,
                request.Status,
                command.UserId));
            await _workflowStore.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _workflowStore.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }
        catch (DbUpdateException)
        {
            _workflowStore.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, "Purchase request workflow could not be saved.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
            PurchaseRequestViewMapper.ToDetailsView(request),
            "Purchase request cancelled.");
    }

    private static bool HasExpectedVersion(string current, string expected)
        => string.Equals(current, expected.Trim(), StringComparison.Ordinal);

    private static PurchaseRequestOperationResult<PurchaseRequestDetailsView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(status, message);
}