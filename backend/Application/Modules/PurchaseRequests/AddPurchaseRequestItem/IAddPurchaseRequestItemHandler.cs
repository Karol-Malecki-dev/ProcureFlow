using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.AddPurchaseRequestItem;

public interface IAddPurchaseRequestItemHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        AddPurchaseRequestItemCommand command,
        CancellationToken cancellationToken = default);
}
