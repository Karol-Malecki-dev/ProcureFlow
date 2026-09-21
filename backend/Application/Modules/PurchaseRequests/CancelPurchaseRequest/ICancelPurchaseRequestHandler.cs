using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.CancelPurchaseRequest;

public interface ICancelPurchaseRequestHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        CancelPurchaseRequestCommand command,
        CancellationToken cancellationToken = default);
}