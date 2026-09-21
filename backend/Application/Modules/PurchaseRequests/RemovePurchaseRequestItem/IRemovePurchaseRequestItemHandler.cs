using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;

public interface IRemovePurchaseRequestItemHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        RemovePurchaseRequestItemCommand command,
        CancellationToken cancellationToken = default);
}
