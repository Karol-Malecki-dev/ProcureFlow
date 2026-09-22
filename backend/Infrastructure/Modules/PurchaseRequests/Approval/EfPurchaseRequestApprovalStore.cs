using Application.Modules.PurchaseRequests;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Approval;

/// <summary>
/// EF adapter for approval queues, monthly budgets and atomic approval writes.
/// </summary>
public sealed class EfPurchaseRequestApprovalStore : IPurchaseRequestApprovalStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestApprovalStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseRequest?> GetManagerRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleOrDefaultAsync(
                request => request.Id == purchaseRequestId
                    && request.OrganizationId == membership.OrganizationId
                    && request.BranchId == membership.BranchId,
                cancellationToken);

    public Task<PurchaseRequest?> GetProcurementRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
        => _dbContext.PurchaseRequests
            .Include(request => request.Items)
            .SingleOrDefaultAsync(
                request => request.Id == purchaseRequestId
                    && request.OrganizationId == membership.OrganizationId,
                cancellationToken);

    public async Task<PurchaseRequestApprovalQueueView> QueryManagerQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Where(request => request.OrganizationId == membership.OrganizationId
                && request.BranchId == membership.BranchId
                && request.Status == PurchaseRequestStatus.Submitted)
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);

        return new PurchaseRequestApprovalQueueView(
            requests.Select(request => ToQueueItem(
                request,
                membership.UserId,
                Domain.Models.Organizations.Enums.BusinessRole.Manager)).ToList(),
            Domain.Models.Organizations.Enums.BusinessRole.Manager);
    }

    public async Task<PurchaseRequestApprovalQueueView> QueryProcurementQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default)
    {
        var requests = await _dbContext.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Items)
            .Where(request => request.OrganizationId == membership.OrganizationId
                && request.Status == PurchaseRequestStatus.AwaitingProcurementApproval)
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .ToListAsync(cancellationToken);

        return new PurchaseRequestApprovalQueueView(
            requests.Select(request => ToQueueItem(
                request,
                membership.UserId,
                Domain.Models.Organizations.Enums.BusinessRole.Procurement)).ToList(),
            Domain.Models.Organizations.Enums.BusinessRole.Procurement);
    }

    public Task<BranchMonthlyBudget?> GetBudgetAsync(
        Guid branchId,
        int year,
        int month,
        CancellationToken cancellationToken = default)
        => _dbContext.BranchMonthlyBudgets
            .SingleOrDefaultAsync(
                budget => budget.BranchId == branchId
                    && budget.Year == year
                    && budget.Month == month,
                cancellationToken);

    public Task<Domain.Models.Organizations.Branch?> GetActiveBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
        => _dbContext.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                branch => branch.Id == branchId
                    && branch.OrganizationId == organizationId
                    && !branch.IsArchived,
                cancellationToken);

    public void AddBudget(BranchMonthlyBudget budget)
        => _dbContext.BranchMonthlyBudgets.Add(budget);

    public void AddDecision(PurchaseRequestApprovalDecision decision)
        => _dbContext.PurchaseRequestApprovalDecisions.Add(decision);

    public void AddStatusHistory(PurchaseRequestStatusHistory history)
        => _dbContext.PurchaseRequestStatusHistories.Add(history);

    public async Task SaveChangesInTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (string.Equals(
            _dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.InMemory",
            StringComparison.Ordinal))
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public void ClearChangeTracker()
        => _dbContext.ChangeTracker.Clear();

    private static PurchaseRequestApprovalQueueItemView ToQueueItem(
        PurchaseRequest request,
        Guid actorUserId,
        Domain.Models.Organizations.Enums.BusinessRole queueRole)
        => new(
            request.Id,
            request.AuthorUserId,
            request.OrganizationId,
            request.BranchId,
            request.Status,
            request.Note,
            request.Items
                .OrderBy(item => item.Id)
                .Select(PurchaseRequestViewMapper.ToItemView)
                .ToList(),
            request.TotalValue,
            request.CreatedAt,
            request.UpdatedAt,
            request.ConcurrencyStamp,
            request.AuthorUserId != actorUserId,
            queueRole);
}
