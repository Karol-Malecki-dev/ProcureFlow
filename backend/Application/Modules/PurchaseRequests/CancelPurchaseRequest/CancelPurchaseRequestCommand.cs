namespace Application.Modules.PurchaseRequests.CancelPurchaseRequest;

/// <summary>Input for cancelling one author-owned purchase request.</summary>
public sealed record CancelPurchaseRequestCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string? ExpectedConcurrencyStamp);