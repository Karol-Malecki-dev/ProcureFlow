using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Budget.GetBranchMonthlyBudget;
using Infrastructure.Data;

namespace Infrastructure.Modules.PurchaseRequests.Budget;

/// <summary>Reads a budget only within the caller's live organization scope.</summary>
public sealed class GetBranchMonthlyBudgetHandler : IGetBranchMonthlyBudgetHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestApprovalStore _store;

    public GetBranchMonthlyBudgetHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestApprovalStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<BranchMonthlyBudgetView>> HandleAsync(
        GetBranchMonthlyBudgetQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty
            || query.OrganizationId == Guid.Empty
            || query.BranchId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Budget identifiers are required.");
        }

        if (!IsValidPeriod(query.Year, query.Month))
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Budget year and month are invalid.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            query.UserId,
            query.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        if (!CanReadBranchBudget(membership, query.BranchId))
        {
            return Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot read this branch budget.");
        }

        if (await _store.GetActiveBranchAsync(query.OrganizationId, query.BranchId, cancellationToken) is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Branch does not exist, is archived, or belongs to another organization.");
        }

        var budget = await _store.GetBudgetAsync(
            query.BranchId,
            query.Year,
            query.Month,
            cancellationToken);
        return budget is null
            ? Failure(PurchaseRequestOperationStatus.NotFound, "Monthly budget was not found.")
            : PurchaseRequestOperationResult<BranchMonthlyBudgetView>.Success(
                PurchaseRequestViewMapper.ToBudgetView(budget, query.OrganizationId));
    }

    private static bool CanReadBranchBudget(
        PurchaseRequestMembership membership,
        Guid branchId)
        => membership.IsActivePlatformAdminScope
            || membership.IsActiveProcurementScope
            || (membership.IsActiveManagerScope && membership.BranchId == branchId);

    private static bool IsValidPeriod(int year, int month)
        => year > 0 && month is >= 1 and <= 12;

    private static PurchaseRequestOperationResult<BranchMonthlyBudgetView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<BranchMonthlyBudgetView>.Failure(status, message);
}
