using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;

public interface IDecidePurchaseRequestHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        DecidePurchaseRequestCommand command,
        CancellationToken cancellationToken = default);
}