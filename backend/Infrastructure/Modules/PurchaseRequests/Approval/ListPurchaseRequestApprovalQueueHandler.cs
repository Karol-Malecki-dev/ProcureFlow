using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Approval.ListPurchaseRequestApprovalQueue;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.PurchaseRequests.Approval;

/// <summary>Resolves and returns the queue owned by the caller's active business role.</summary>
public sealed class ListPurchaseRequestApprovalQueueHandler : IListPurchaseRequestApprovalQueueHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestApprovalStore _store;

    public ListPurchaseRequestApprovalQueueHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestApprovalStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView>> HandleAsync(
        ListPurchaseRequestApprovalQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty || query.OrganizationId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Approval queue identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            query.UserId,
            query.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        if (membership.IsActiveManagerScope)
        {
            return PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView>.Success(
                await _store.QueryManagerQueueAsync(membership, cancellationToken));
        }

        if (membership.IsActiveProcurementScope)
        {
            return PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView>.Success(
                await _store.QueryProcurementQueueAsync(membership, cancellationToken));
        }

        return Failure(
            membership.Role is BusinessRole.Manager or BusinessRole.Procurement
                ? PurchaseRequestOperationStatus.Conflict
                : PurchaseRequestOperationStatus.Forbidden,
            "The current user cannot access an approval queue in this scope.");
    }

    private static PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestApprovalQueueView>.Failure(status, message);
}
