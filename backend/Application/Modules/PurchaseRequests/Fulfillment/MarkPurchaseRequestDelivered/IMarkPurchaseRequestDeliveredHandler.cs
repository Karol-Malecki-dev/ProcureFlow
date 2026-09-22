namespace Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestDelivered;

public interface IMarkPurchaseRequestDeliveredHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        MarkPurchaseRequestDeliveredCommand command,
        CancellationToken cancellationToken = default);
}
