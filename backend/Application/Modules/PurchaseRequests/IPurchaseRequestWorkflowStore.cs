using Domain.Entities;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Persistence port for author-owned purchase-request lifecycle transitions.
/// </summary>
public interface IPurchaseRequestWorkflowStore
{
    Task<PurchaseRequest?> GetOwnedAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    void AddStatusHistory(PurchaseRequestStatusHistory history);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}