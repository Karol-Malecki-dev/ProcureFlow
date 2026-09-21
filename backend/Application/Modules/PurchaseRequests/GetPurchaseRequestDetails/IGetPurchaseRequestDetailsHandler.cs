using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;

public interface IGetPurchaseRequestDetailsHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        GetPurchaseRequestDetailsQuery query,
        CancellationToken cancellationToken = default);
}
