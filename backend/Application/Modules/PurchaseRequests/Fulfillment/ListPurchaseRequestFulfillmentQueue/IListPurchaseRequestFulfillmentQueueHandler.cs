namespace Application.Modules.PurchaseRequests.Fulfillment.ListPurchaseRequestFulfillmentQueue;

public interface IListPurchaseRequestFulfillmentQueueHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestFulfillmentQueueView>> HandleAsync(
        ListPurchaseRequestFulfillmentQueueQuery query,
        CancellationToken cancellationToken = default);
}
