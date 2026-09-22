using Application.Modules.PurchaseRequests;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Fulfillment;

/// <summary>
/// EF adapter for the Procurement fulfillment queue and lifecycle transitions.
/// </summary>
public sealed class EfPurchaseRequestFulfillmentStore : IPurchaseRequestFulfillmentStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestFulfillmentStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PurchaseRequestFulfillmentQueueView> QueryQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Where(request => request.OrganizationId == membership.OrganizationId
                && (request.Status == PurchaseRequestStatus.Approved
                    || request.Status == PurchaseRequestStatus.Ordered))
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);

        return new PurchaseRequestFulfillmentQueueView(
            requests.Select(PurchaseRequestViewMapper.ToFulfillmentQueueItemView).ToList());
    }

    public Task<PurchaseRequest?> GetRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleOrDefaultAsync(
                request => request.Id == purchaseRequestId
                    && request.OrganizationId == membership.OrganizationId
                    && (request.Status == PurchaseRequestStatus.Approved
                        || request.Status == PurchaseRequestStatus.Ordered),
                cancellationToken);

    public void AddStatusHistory(PurchaseRequestStatusHistory history)
        => _dbContext.PurchaseRequestStatusHistories.Add(history);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void ClearChangeTracker()
        => _dbContext.ChangeTracker.Clear();
}
