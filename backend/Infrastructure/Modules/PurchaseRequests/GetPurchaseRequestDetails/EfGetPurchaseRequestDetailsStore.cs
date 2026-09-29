using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.GetPurchaseRequestDetails;

public sealed class EfGetPurchaseRequestDetailsStore : IGetPurchaseRequestDetailsStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfGetPurchaseRequestDetailsStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseRequestDetailsView?> QueryAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.PurchaseRequests
            .AsNoTracking()
            .Include(candidate => candidate.Items)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == purchaseRequestId
                    && candidate.AuthorUserId == membership.UserId
                    && candidate.OrganizationId == membership.OrganizationId
                    && candidate.BranchId == membership.BranchId,
                cancellationToken);

            if (request is null)
            {
                return null;
            }

            var history = await (
                from statusHistory in _dbContext.PurchaseRequestStatusHistories.AsNoTracking()
                join user in _dbContext.Users.AsNoTracking()
                    on statusHistory.ChangedByUserId equals user.Id
                where statusHistory.PurchaseRequestId == purchaseRequestId
                orderby statusHistory.ChangedAt, statusHistory.Id
                select new PurchaseRequestStatusHistoryView(
                    statusHistory.Id,
                    statusHistory.FromStatus,
                    statusHistory.ToStatus,
                    statusHistory.ChangedByUserId,
                    user.DisplayName.Value,
                    statusHistory.ChangedAt))
                .ToListAsync(cancellationToken);

            return PurchaseRequestViewMapper.ToDetailsView(request, history);
    }
}
