using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;

/// <summary>
/// Updates one item quantity through the request aggregate and optimistic concurrency boundary.
/// </summary>
public sealed class UpdatePurchaseRequestItemQuantityHandler : IUpdatePurchaseRequestItemQuantityHandler
{
    private const string ConcurrencyConflictMessage = "Purchase request was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestDraftStore _draftStore;

    public UpdatePurchaseRequestItemQuantityHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestDraftStore draftStore)
    {
        _membershipReader = membershipReader;
        _draftStore = draftStore;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        UpdatePurchaseRequestItemQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request concurrency stamp is required.");
        }

        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.PurchaseRequestId == Guid.Empty
            || command.ItemId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Purchase request and item identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(command.UserId, command.OrganizationId, cancellationToken);
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
                "The current user cannot edit purchase request drafts in this scope.");
        }

        var request = await _draftStore.GetOwnedDraftAsync(membership, command.PurchaseRequestId, cancellationToken);
        if (request is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Purchase request draft was not found.");
        }

        if (!string.Equals(request.ConcurrencyStamp, command.ExpectedConcurrencyStamp.Trim(), StringComparison.Ordinal))
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }

        if (!request.Items.Any(item => item.Id == command.ItemId))
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Purchase request item was not found.");
        }

        try
        {
            request.UpdateItemQuantity(command.ItemId, command.Quantity);
            await _draftStore.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(PurchaseRequestOperationStatus.Conflict, exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _draftStore.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }

        return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(
            PurchaseRequestViewMapper.ToDetailsView(request),
            "Purchase request item quantity updated.");
    }

    private static PurchaseRequestOperationResult<PurchaseRequestDetailsView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(status, message);
}
