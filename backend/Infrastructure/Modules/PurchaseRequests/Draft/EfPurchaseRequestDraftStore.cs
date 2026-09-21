using Application.Modules.PurchaseRequests;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Draft;

public sealed class EfPurchaseRequestDraftStore : IPurchaseRequestDraftStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestDraftStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseRequest?> GetOwnedDraftAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleOrDefaultAsync(
                request => request.Id == purchaseRequestId
                    && request.AuthorUserId == membership.UserId
                    && request.OrganizationId == membership.OrganizationId
                    && request.BranchId == membership.BranchId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void ClearChangeTracker()
        => _dbContext.ChangeTracker.Clear();
}
