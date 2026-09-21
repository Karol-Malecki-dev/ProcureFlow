using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.SubmitPurchaseRequest;

public interface ISubmitPurchaseRequestHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        SubmitPurchaseRequestCommand command,
        CancellationToken cancellationToken = default);
}