using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.ListMyPurchaseRequests;

public interface IListMyPurchaseRequestsStore
{
    Task<PurchaseRequestListView> QueryAsync(
        PurchaseRequestMembership membership,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
