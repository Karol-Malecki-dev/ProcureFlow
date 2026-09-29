using Application.Modules.PurchaseRequests;
using Domain.Enums;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests;

/// <summary>
/// EF Core projections for the purchase-request dashboard.
/// Each metric is aggregated by PostgreSQL and only the small result set is materialized.
/// </summary>
public sealed class EfPurchaseRequestDashboardStore : IPurchaseRequestDashboardStore
{
    private const int PopularProductLimit = 5;
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestDashboardStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseRequestDashboardView> QueryAsync(
        PurchaseRequestDashboardScope scope,
        DateTime monthStartUtc,
        DateTime nextMonthStartUtc,
        CancellationToken cancellationToken = default)
    {
        var scopedRequests = _dbContext.PurchaseRequests
            .AsNoTracking()
            .Where(request => request.OrganizationId == scope.OrganizationId);

        if (!scope.IsOrganizationWide && scope.Role == BusinessRole.Employee)
        {
            scopedRequests = scopedRequests.Where(request =>
                request.AuthorUserId == scope.UserId
                && request.BranchId == scope.BranchId);
        }
        else if (!scope.IsOrganizationWide)
        {
            scopedRequests = scopedRequests.Where(request => request.BranchId == scope.BranchId);
        }

        var pendingStatuses = GetPendingStatuses(scope.Role);
        var pendingRequestsCount = await scopedRequests
            .CountAsync(request => pendingStatuses.Contains(request.Status), cancellationToken);

        var orderedRequestIds = _dbContext.PurchaseRequestStatusHistories
            .AsNoTracking()
            .Where(history => history.ToStatus == PurchaseRequestStatus.Ordered
                && history.ChangedAt >= monthStartUtc
                && history.ChangedAt < nextMonthStartUtc)
            .Select(history => history.PurchaseRequestId);

        var orderedRequests = scopedRequests
            .Where(request => orderedRequestIds.Contains(request.Id));

        var currentMonthOrderValue = await orderedRequests
            .Select(request => (decimal?)request.TotalValue)
            .SumAsync(cancellationToken) ?? 0m;

        var popularProducts = await (
            from item in _dbContext.PurchaseRequestItems.AsNoTracking()
            join request in orderedRequests on item.PurchaseRequestId equals request.Id
            join product in _dbContext.Products.AsNoTracking()
                on item.ProductId equals product.Id
            group item by new
            {
                product.Id,
                product.Name,
                product.Code
            }
            into productGroup
            orderby productGroup.Count() descending, productGroup.Sum(item => item.Quantity) descending,
                productGroup.Key.Name
            select new PurchaseRequestDashboardProductView(
                productGroup.Key.Id,
                productGroup.Key.Name,
                productGroup.Key.Code,
                productGroup.Sum(item => item.Quantity),
                productGroup.Count()))
            .Take(PopularProductLimit)
            .ToListAsync(cancellationToken);

        var spendingByBranch = await (
            from request in orderedRequests
            join branch in _dbContext.Branches.AsNoTracking()
                on request.BranchId equals branch.Id
            group request by new
            {
                branch.Id,
                branch.Name
            }
            into branchGroup
            orderby branchGroup.Sum(request => request.TotalValue) descending, branchGroup.Key.Name
            select new PurchaseRequestDashboardBranchView(
                branchGroup.Key.Id,
                branchGroup.Key.Name,
                branchGroup.Sum(request => request.TotalValue)))
            .ToListAsync(cancellationToken);

        return new PurchaseRequestDashboardView(
            scope.Role,
            pendingRequestsCount,
            currentMonthOrderValue,
            popularProducts,
            spendingByBranch);
    }

    private static PurchaseRequestStatus[] GetPendingStatuses(BusinessRole role)
        => role switch
        {
            BusinessRole.Manager => [PurchaseRequestStatus.Submitted],
            BusinessRole.Employee =>
            [
                PurchaseRequestStatus.Submitted,
                PurchaseRequestStatus.AwaitingProcurementApproval,
                PurchaseRequestStatus.Approved,
                PurchaseRequestStatus.Ordered
            ],
            BusinessRole.Procurement =>
            [
                PurchaseRequestStatus.AwaitingProcurementApproval,
                PurchaseRequestStatus.Approved,
                PurchaseRequestStatus.Ordered
            ],
            _ => []
        };
}