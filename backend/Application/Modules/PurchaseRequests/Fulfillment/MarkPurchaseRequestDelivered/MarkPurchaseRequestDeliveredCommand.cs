namespace Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestDelivered;

/// <summary>Marks one ordered request as delivered by Procurement.</summary>
public sealed record MarkPurchaseRequestDeliveredCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string? ExpectedConcurrencyStamp,
    string? FulfillmentNote);
