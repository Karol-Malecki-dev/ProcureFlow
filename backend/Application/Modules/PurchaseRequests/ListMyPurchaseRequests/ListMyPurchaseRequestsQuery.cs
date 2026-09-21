namespace Application.Modules.PurchaseRequests.ListMyPurchaseRequests;

/// <summary>Input for the current Employee's stable request list.</summary>
public sealed record ListMyPurchaseRequestsQuery(
    Guid UserId,
    Guid OrganizationId,
    int Page = 1,
    int PageSize = 20);
