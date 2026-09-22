namespace Application.Modules.PurchaseRequests.Budget.UpsertBranchMonthlyBudget;

/// <summary>Creates or updates a branch monthly limit using the expected budget version.</summary>
public sealed record UpsertBranchMonthlyBudgetCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid BranchId,
    int Year,
    int Month,
    decimal LimitAmount,
    string? ExpectedConcurrencyStamp);