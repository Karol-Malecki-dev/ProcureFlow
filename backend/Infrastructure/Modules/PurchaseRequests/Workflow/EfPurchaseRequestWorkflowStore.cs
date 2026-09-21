using Application.Modules.PurchaseRequests;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Workflow;

/// <summary>
/// EF persistence adapter for author-owned request lifecycle transitions.
/// </summary>
public sealed class EfPurchaseRequestWorkflowStore : IPurchaseRequestWorkflowStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestWorkflowStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseRequest?> GetOwnedAsync(
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

    public void AddStatusHistory(PurchaseRequestStatusHistory history)
        => _dbContext.PurchaseRequestStatusHistories.Add(history);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void ClearChangeTracker()
        => _dbContext.ChangeTracker.Clear();
}