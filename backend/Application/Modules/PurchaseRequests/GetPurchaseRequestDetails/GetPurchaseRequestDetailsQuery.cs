namespace Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;

/// <summary>Input for reading one author-owned request draft.</summary>
public sealed record GetPurchaseRequestDetailsQuery(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId);
