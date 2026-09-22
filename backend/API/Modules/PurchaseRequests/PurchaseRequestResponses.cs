using Domain.Enums;
using Domain.Models.Organizations.Enums;

namespace API.Modules.PurchaseRequests;

/// <summary>HTTP response for one historical request item.</summary>
public sealed record PurchaseRequestItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ProductCode,
    string UnitName,
    string UnitSymbol,
    decimal UnitPrice,
    decimal Quantity,
    string? Comment,
    decimal LineTotal);

/// <summary>HTTP response for one purchase-request draft.</summary>
public sealed record PurchaseRequestResponse(
    Guid Id,
    Guid AuthorUserId,
    Guid OrganizationId,
    Guid BranchId,
    PurchaseRequestStatus Status,
    string? Note,
    string? FulfillmentOrderNumber,
    string? FulfillmentNote,
    IReadOnlyList<PurchaseRequestItemResponse> Items,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp);

/// <summary>HTTP response for one entry in the current user's request list.</summary>
public sealed record PurchaseRequestListItemResponse(
    Guid Id,
    PurchaseRequestStatus Status,
    string? Note,
    int ItemCount,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp);

/// <summary>HTTP response for a paginated request list.</summary>
public sealed record PurchaseRequestListResponse(
    IReadOnlyList<PurchaseRequestListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

/// <summary>HTTP response for one branch-month budget.</summary>
public sealed record BranchMonthlyBudgetResponse(
    Guid Id,
    Guid OrganizationId,
    Guid BranchId,
    int Year,
    int Month,
    decimal LimitAmount,
    decimal UsedAmount,
    decimal AvailableAmount,
    string ConcurrencyStamp);

/// <summary>HTTP response for one request in an approval queue.</summary>
public sealed record PurchaseRequestApprovalQueueItemResponse(
    Guid Id,
    Guid AuthorUserId,
    Guid OrganizationId,
    Guid BranchId,
    PurchaseRequestStatus Status,
    string? Note,
    IReadOnlyList<PurchaseRequestItemResponse> Items,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp,
    bool CanDecide,
    BusinessRole QueueRole);

/// <summary>HTTP response for the current user's approval queue.</summary>
public sealed record PurchaseRequestApprovalQueueResponse(
    IReadOnlyList<PurchaseRequestApprovalQueueItemResponse> Items,
    BusinessRole QueueRole);

/// <summary>HTTP response for one accepted request awaiting Procurement fulfillment.</summary>
public sealed record PurchaseRequestFulfillmentQueueItemResponse(
    Guid Id,
    Guid AuthorUserId,
    Guid OrganizationId,
    Guid BranchId,
    PurchaseRequestStatus Status,
    string? Note,
    string? FulfillmentOrderNumber,
    string? FulfillmentNote,
    IReadOnlyList<PurchaseRequestItemResponse> Items,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp);

/// <summary>HTTP response for the Procurement fulfillment queue.</summary>
public sealed record PurchaseRequestFulfillmentQueueResponse(
    IReadOnlyList<PurchaseRequestFulfillmentQueueItemResponse> Items);
