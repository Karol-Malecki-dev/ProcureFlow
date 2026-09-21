using Domain.Enums;

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
