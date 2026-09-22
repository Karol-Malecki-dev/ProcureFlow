namespace Application.Modules.PurchaseRequests.Fulfillment.ListPurchaseRequestFulfillmentQueue;

/// <summary>Requests the accepted purchase requests awaiting Procurement fulfillment.</summary>
public sealed record ListPurchaseRequestFulfillmentQueueQuery(
    Guid UserId,
    Guid OrganizationId);
