using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.Budget.UpsertBranchMonthlyBudget;

public interface IUpsertBranchMonthlyBudgetHandler
{
    Task<PurchaseRequestOperationResult<BranchMonthlyBudgetView>> HandleAsync(
        UpsertBranchMonthlyBudgetCommand command,
        CancellationToken cancellationToken = default);
}