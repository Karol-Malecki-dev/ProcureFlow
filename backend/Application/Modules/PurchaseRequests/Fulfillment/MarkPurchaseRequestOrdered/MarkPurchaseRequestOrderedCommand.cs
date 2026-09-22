namespace Application.Modules.PurchaseRequests.Fulfillment.MarkPurchaseRequestOrdered;

/// <summary>Marks one approved request as ordered by Procurement.</summary>
public sealed record MarkPurchaseRequestOrderedCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string? ExpectedConcurrencyStamp,
    string? OrderNumber,
    string? FulfillmentNote);
