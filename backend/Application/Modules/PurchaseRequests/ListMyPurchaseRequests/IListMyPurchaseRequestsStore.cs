using Application.Modules.PurchaseRequests;
using Domain.Enums;

namespace Application.Modules.PurchaseRequests.ListMyPurchaseRequests;

public interface IListMyPurchaseRequestsStore
{
    Task<PurchaseRequestListView> QueryAsync(
        PurchaseRequestMembership membership,
        int page,
        int pageSize,
        PurchaseRequestStatus? status = null,
        CancellationToken cancellationToken = default);
}
