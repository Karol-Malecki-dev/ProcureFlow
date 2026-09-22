namespace Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestOrdered;

public interface IMarkPurchaseRequestOrderedHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        MarkPurchaseRequestOrderedCommand command,
        CancellationToken cancellationToken = default);
}
