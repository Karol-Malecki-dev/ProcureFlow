namespace API.Modules.PurchaseRequests.AddPurchaseRequestItem;

/// <summary>Client input for adding a catalog product to a draft.</summary>
public sealed record AddPurchaseRequestItemRequest(
    Guid ProductId,
    decimal Quantity,
    string? Comment,
    string ConcurrencyStamp);
