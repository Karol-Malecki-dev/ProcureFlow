namespace API.Modules.PurchaseRequests.Approval;

/// <summary>
/// Client input for approving or rejecting a purchase request.
/// </summary>
public sealed record DecidePurchaseRequestRequest(
    string ConcurrencyStamp,
    bool Approve,
    string? RejectionReason);
