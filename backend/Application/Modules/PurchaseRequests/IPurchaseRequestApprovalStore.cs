using Domain.Entities;
using Domain.Models.Organizations;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Persistence port for approval queues, budgets and atomic approval decisions.
/// </summary>
public interface IPurchaseRequestApprovalStore
{
    Task<PurchaseRequest?> GetManagerRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequest?> GetProcurementRequestAsync(
        PurchaseRequestMembership membership,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestApprovalQueueView> QueryManagerQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestApprovalQueueView> QueryProcurementQueueAsync(
        PurchaseRequestMembership membership,
        CancellationToken cancellationToken = default);

    Task<BranchMonthlyBudget?> GetBudgetAsync(
        Guid branchId,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<Branch?> GetActiveBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    void AddBudget(BranchMonthlyBudget budget);

    void AddDecision(PurchaseRequestApprovalDecision decision);

    void AddStatusHistory(PurchaseRequestStatusHistory history);

    Task SaveChangesInTransactionAsync(CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}