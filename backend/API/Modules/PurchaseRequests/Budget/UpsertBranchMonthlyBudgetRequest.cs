namespace API.Modules.PurchaseRequests.Budget;

/// <summary>Client input for creating or changing one branch-month budget limit.</summary>
public sealed record UpsertBranchMonthlyBudgetRequest(
    decimal LimitAmount,
    string? ExpectedConcurrencyStamp);
