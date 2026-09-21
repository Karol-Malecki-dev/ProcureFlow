using Domain.Entities;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Persistence port for loading and saving an author-owned draft aggregate.
/// </summary>
public interface IPurchaseRequestDraftStore
{
    Task<PurchaseRequest?> GetOwnedDraftAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}
