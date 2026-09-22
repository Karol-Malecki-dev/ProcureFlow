namespace Application.Modules.PurchaseRequests.Budget.GetBranchMonthlyBudget;

/// <summary>Reads one monthly budget after validating the caller's organization scope.</summary>
public sealed record GetBranchMonthlyBudgetQuery(
    Guid UserId,
    Guid OrganizationId,
    Guid BranchId,
    int Year,
    int Month);