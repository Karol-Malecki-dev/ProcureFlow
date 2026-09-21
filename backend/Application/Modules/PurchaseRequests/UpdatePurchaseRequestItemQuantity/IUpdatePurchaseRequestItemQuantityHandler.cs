using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;

public interface IUpdatePurchaseRequestItemQuantityHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        UpdatePurchaseRequestItemQuantityCommand command,
        CancellationToken cancellationToken = default);
}
