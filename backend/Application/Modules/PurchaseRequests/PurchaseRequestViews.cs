using Domain.Enums;

namespace Application.Modules.PurchaseRequests;

/// <summary>Read-only projection of one request item.</summary>
public sealed record PurchaseRequestItemView(
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

/// <summary>Read-only projection of request details.</summary>
public sealed record PurchaseRequestDetailsView(
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
    string ConcurrencyStamp);

/// <summary>Read-only projection used by the current user's request list.</summary>
public sealed record PurchaseRequestListItemView(
    Guid Id,
    PurchaseRequestStatus Status,
    string? Note,
    int ItemCount,
    decimal TotalValue,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string ConcurrencyStamp);

/// <summary>Stable paginated request-list result.</summary>
public sealed record PurchaseRequestListView(
    IReadOnlyList<PurchaseRequestListItemView> Items,
    int Page,
    int PageSize,
    int TotalCount);
