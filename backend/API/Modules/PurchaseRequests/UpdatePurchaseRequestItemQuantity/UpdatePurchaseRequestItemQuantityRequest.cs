namespace API.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;

/// <summary>Client input for changing an item quantity using an expected version.</summary>
public sealed record UpdatePurchaseRequestItemQuantityRequest(
    decimal Quantity,
    string ConcurrencyStamp);
