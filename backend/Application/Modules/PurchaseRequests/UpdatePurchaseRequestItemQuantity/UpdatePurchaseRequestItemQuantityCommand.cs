namespace Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;

/// <summary>Input for changing one item quantity in an owned draft.</summary>
public sealed record UpdatePurchaseRequestItemQuantityCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    Guid ItemId,
    decimal Quantity,
    string? ExpectedConcurrencyStamp);
