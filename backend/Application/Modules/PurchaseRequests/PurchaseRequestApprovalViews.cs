using Domain.Enums;
using Domain.Models.Organizations.Enums;

namespace Application.Modules.PurchaseRequests;

/// <summary>Read-only projection of one branch's monthly budget.</summary>
public sealed record BranchMonthlyBudgetView(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    int Year,
    int Month,
    decimal LimitAmount,
    decimal UsedAmount,
    decimal AvailableAmount,
    string ConcurrencyStamp);

/// <summary>Read-only projection used by Manager and Procurement approval queues.</summary>
public sealed record PurchaseRequestApprovalQueueItemView(
    Guid Id,
    Guid AuthorUserId,
    Guid OrganizationId,
    Guid BranchId,
    PurchaseRequestStatus Status,
    string? Note,
    IReadOnlyList<PurchaseRequestItemView> Items,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp,
    bool CanDecide,
    BusinessRole QueueRole);

/// <summary>Approval queue returned for one authorized business role.</summary>
public sealed record PurchaseRequestApprovalQueueView(
    IReadOnlyList<PurchaseRequestApprovalQueueItemView> Items,
    BusinessRole QueueRole);
