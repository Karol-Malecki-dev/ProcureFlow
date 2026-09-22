namespace Application.Modules.PurchaseRequests.Approval.ListPurchaseRequestApprovalQueue;

/// <summary>Lists the queue owned by the caller's active Manager or Procurement membership.</summary>
public sealed record ListPurchaseRequestApprovalQueueQuery(
    Guid UserId,
    Guid OrganizationId);