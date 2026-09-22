namespace Application.Modules.PurchaseRequests.Approval.DecidePurchaseRequest;

/// <summary>Approves or rejects one request from the caller's authorized approval queue.</summary>
public sealed record DecidePurchaseRequestCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string? ExpectedConcurrencyStamp,
    bool Approve,
    string? RejectionReason);