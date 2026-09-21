namespace API.Modules.PurchaseRequests.CreatePurchaseRequest;

/// <summary>Client-controlled fields allowed when creating an empty draft.</summary>
public sealed record CreatePurchaseRequestRequest(
    string? Note);
