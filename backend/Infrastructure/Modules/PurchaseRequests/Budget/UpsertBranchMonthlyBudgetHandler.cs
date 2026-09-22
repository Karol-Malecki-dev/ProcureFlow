using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Budget.UpsertBranchMonthlyBudget;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.Budget;

/// <summary>Creates or changes a monthly branch limit for Procurement or an active platform admin.</summary>
public sealed class UpsertBranchMonthlyBudgetHandler : IUpsertBranchMonthlyBudgetHandler
{
    private const string ConcurrencyConflictMessage = "Monthly budget was modified concurrently; refresh and retry.";

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestApprovalStore _store;

    public UpsertBranchMonthlyBudgetHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestApprovalStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<BranchMonthlyBudgetView>> HandleAsync(
        UpsertBranchMonthlyBudgetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty
            || command.OrganizationId == Guid.Empty
            || command.BranchId == Guid.Empty)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Budget identifiers are required.");
        }

        if (command.Year <= 0 || command.Month is < 1 or > 12)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, "Budget year and month are invalid.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            command.UserId,
            command.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(PurchaseRequestOperationStatus.NotFound, "Active organization membership was not found.");
        }

        if (!membership.CanManageBudgets)
        {
            return Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "Only Procurement or an active platform admin can manage branch budgets.");
        }

        if (await _store.GetActiveBranchAsync(command.OrganizationId, command.BranchId, cancellationToken) is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Branch does not exist, is archived, or belongs to another organization.");
        }

        var budget = await _store.GetBudgetAsync(
            command.BranchId,
            command.Year,
            command.Month,
            cancellationToken);
        var created = budget is null;

        try
        {
            if (budget is null)
            {
                budget = BranchMonthlyBudget.Create(
                    command.BranchId,
                    command.Year,
                    command.Month,
                    command.LimitAmount);
                _store.AddBudget(budget);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(command.ExpectedConcurrencyStamp))
                {
                    return Failure(
                        PurchaseRequestOperationStatus.ValidationError,
                        "Monthly budget concurrency stamp is required when updating an existing budget.");
                }

                if (!HasExpectedVersion(budget.ConcurrencyStamp, command.ExpectedConcurrencyStamp))
                {
                    return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
                }

                budget.ChangeLimit(command.LimitAmount);
            }

            await _store.SaveChangesInTransactionAsync(cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Failure(PurchaseRequestOperationStatus.ValidationError, exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, ConcurrencyConflictMessage);
        }
        catch (DbUpdateException exception) when (
            PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                exception,
                "UX_BranchMonthlyBudgets_Branch_Period"))
        {
            _store.ClearChangeTracker();
            return Failure(
                PurchaseRequestOperationStatus.Conflict,
                "A monthly budget already exists for this branch and period.");
        }
        catch (DbUpdateException)
        {
            _store.ClearChangeTracker();
            return Failure(PurchaseRequestOperationStatus.Conflict, "Monthly budget could not be saved.");
        }

        return PurchaseRequestOperationResult<BranchMonthlyBudgetView>.Success(
            PurchaseRequestViewMapper.ToBudgetView(budget, command.OrganizationId),
            created ? "Monthly budget created." : "Monthly budget limit updated.");
    }

    private static bool HasExpectedVersion(string current, string expected)
        => string.Equals(current, expected.Trim(), StringComparison.Ordinal);

    private static PurchaseRequestOperationResult<BranchMonthlyBudgetView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<BranchMonthlyBudgetView>.Failure(status, message);
}
