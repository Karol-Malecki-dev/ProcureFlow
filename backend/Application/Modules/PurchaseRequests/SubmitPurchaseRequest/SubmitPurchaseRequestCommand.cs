namespace Application.Modules.PurchaseRequests.SubmitPurchaseRequest;

/// <summary>Input for submitting one author-owned purchase request.</summary>
public sealed record SubmitPurchaseRequestCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string? ExpectedConcurrencyStamp);