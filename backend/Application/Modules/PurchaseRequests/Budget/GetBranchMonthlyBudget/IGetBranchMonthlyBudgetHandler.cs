using Application.Modules.PurchaseRequests;

namespace Application.Modules.PurchaseRequests.Budget.GetBranchMonthlyBudget;

public interface IGetBranchMonthlyBudgetHandler
{
    Task<PurchaseRequestOperationResult<BranchMonthlyBudgetView>> HandleAsync(
        GetBranchMonthlyBudgetQuery query,
        CancellationToken cancellationToken = default);
}