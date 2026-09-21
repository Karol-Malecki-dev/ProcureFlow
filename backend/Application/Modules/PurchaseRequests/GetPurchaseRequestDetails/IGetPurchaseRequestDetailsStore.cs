using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;

public interface IGetPurchaseRequestDetailsStore
{
    Task<PurchaseRequestDetailsView?> QueryAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);
}
