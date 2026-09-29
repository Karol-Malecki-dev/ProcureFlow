using Domain.Enums;
using Domain.Models.Organizations.Enums;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Scope resolved from the current user's active membership for dashboard reads.
/// </summary>
public sealed record PurchaseRequestDashboardScope(
    Guid UserId,
    Guid OrganizationId,
    Guid? BranchId,
    BusinessRole Role,
    bool IsOrganizationWide);

/// <summary>Read-only dashboard projection for the current organization scope.</summary>
public sealed record PurchaseRequestDashboardView(
    BusinessRole ScopeRole,
    int PendingRequestsCount,
    decimal CurrentMonthOrderValue,
    IReadOnlyList<PurchaseRequestDashboardProductView> MostFrequentlyOrderedProducts,
    IReadOnlyList<PurchaseRequestDashboardBranchView> SpendingByBranch);

/// <summary>Aggregated product usage for requests ordered during the selected month.</summary>
public sealed record PurchaseRequestDashboardProductView(
    Guid ProductId,
    string ProductName,
    string? ProductCode,
    decimal TotalQuantity,
    int RequestCount);

/// <summary>Aggregated order value for one branch during the selected month.</summary>
public sealed record PurchaseRequestDashboardBranchView(
    Guid BranchId,
    string BranchName,
    decimal TotalValue);

/// <summary>Query for the current user's purchase-request dashboard.</summary>
public sealed record GetPurchaseRequestDashboardQuery(
    Guid UserId,
    Guid OrganizationId);

/// <summary>Application handler for the purchase-request dashboard read slice.</summary>
public interface IGetPurchaseRequestDashboardHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestDashboardView>> HandleAsync(
        GetPurchaseRequestDashboardQuery query,
        CancellationToken cancellationToken = default);
}

/// <summary>Persistence port for SQL-backed purchase-request dashboard projections.</summary>
public interface IPurchaseRequestDashboardStore
{
    Task<PurchaseRequestDashboardView> QueryAsync(
        PurchaseRequestDashboardScope scope,
        DateTime monthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default);
}