namespace Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;

/// <summary>Input for removing one item from an owned draft.</summary>
public sealed record RemovePurchaseRequestItemCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    Guid ItemId,
    string? ExpectedConcurrencyStamp);
