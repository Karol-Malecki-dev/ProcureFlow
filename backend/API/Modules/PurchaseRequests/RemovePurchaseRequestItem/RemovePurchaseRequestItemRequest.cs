namespace API.Modules.PurchaseRequests.RemovePurchaseRequestItem;

/// <summary>Client input for removing an item using an expected version.</summary>
public sealed record RemovePurchaseRequestItemRequest(
    string ConcurrencyStamp);
