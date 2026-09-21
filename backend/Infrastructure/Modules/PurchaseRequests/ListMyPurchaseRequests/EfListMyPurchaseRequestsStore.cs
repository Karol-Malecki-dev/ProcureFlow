using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;

public sealed class EfListMyPurchaseRequestsStore : IListMyPurchaseRequestsStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfListMyPurchaseRequestsStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseRequestListView> QueryAsync(
        PurchaseRequestMembership membership,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PurchaseRequests
            .AsNoTracking()
            .Where(request => request.AuthorUserId == membership.UserId
                && request.OrganizationId == membership.OrganizationId
                && request.BranchId == membership.BranchId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new PurchaseRequestListItemView(
                request.Id,
                request.Status,
                request.Note,
                request.Items.Count,
                request.TotalValue,
                request.CreatedAt,
                request.UpdatedAt,
                request.ConcurrencyStamp))
            .ToListAsync(cancellationToken);

        return new PurchaseRequestListView(items, page, pageSize, totalCount);
    }
}
