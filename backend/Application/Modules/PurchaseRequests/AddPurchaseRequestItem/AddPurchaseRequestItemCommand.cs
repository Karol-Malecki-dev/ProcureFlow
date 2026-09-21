namespace Application.Modules.PurchaseRequests.AddPurchaseRequestItem;

/// <summary>Input for adding one catalog snapshot to an owned draft.</summary>
public sealed record AddPurchaseRequestItemCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    Guid ProductId,
    decimal Quantity,
    string? Comment,
    string? ExpectedConcurrencyStamp);
