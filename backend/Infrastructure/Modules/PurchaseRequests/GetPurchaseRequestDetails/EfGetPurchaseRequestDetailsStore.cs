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

        return request is null
            ? null
            : PurchaseRequestViewMapper.ToDetailsView(request);
    }
}
