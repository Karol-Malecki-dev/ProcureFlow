namespace API.Modules.PurchaseRequests.ChangePurchaseRequestStatus;

/// <summary>Client input for a purchase-request lifecycle transition.</summary>
public sealed record PurchaseRequestStatusChangeRequest(string ConcurrencyStamp);