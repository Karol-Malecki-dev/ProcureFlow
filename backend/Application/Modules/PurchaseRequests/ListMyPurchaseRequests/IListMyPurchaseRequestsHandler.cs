using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.ListMyPurchaseRequests;

public interface IListMyPurchaseRequestsHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestListView>> HandleAsync(
        ListMyPurchaseRequestsQuery query,
        CancellationToken cancellationToken = default);
}
