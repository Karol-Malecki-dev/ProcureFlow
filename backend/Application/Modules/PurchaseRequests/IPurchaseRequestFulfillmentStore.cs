using Domain.Entities;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Persistence port for the Procurement fulfillment queue and lifecycle transitions.
/// </summary>
public interface IPurchaseRequestFulfillmentStore
{
    Task<PurchaseRequestFulfillmentQueueView> QueryQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequest?> GetRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    void AddStatusHistory(PurchaseRequestStatusHistory history);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}
