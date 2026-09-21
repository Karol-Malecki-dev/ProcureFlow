using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.CreatePurchaseRequest;

public interface ICreatePurchaseRequestHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        CreatePurchaseRequestCommand command,
        CancellationToken cancellationToken = default);
}
