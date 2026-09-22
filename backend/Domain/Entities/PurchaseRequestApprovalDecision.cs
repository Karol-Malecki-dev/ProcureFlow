using Domain.Enums;
using Domain.Models.Organizations.Enums;

namespace Domain.Entities;

/// <summary>
/// Immutable decision record for one purchase-request approval stage.
/// </summary>
public sealed class PurchaseRequestApprovalDecision
{
    public const int ReasonMaxLength = PurchaseRequest.DecisionReasonMaxLength;

    private PurchaseRequestApprovalDecision()
    {
    }

    private PurchaseRequestApprovalDecision(
        Guid purchaseRequestId,
        Guid actorUserId,
        BusinessRole actorRole,
        PurchaseRequestDecisionType decision,
        decimal requestAmount,
        decimal? availableBudget,
        decimal overBudgetAmount,
        string? reason,
        int? budgetYear,
        int? budgetMonth,
        DateTime decidedAtUtc)
    {
        Id = Guid.NewGuid();
        PurchaseRequestId = RequireIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        ActorUserId = RequireIdentifier(actorUserId, nameof(actorUserId));
        if (!Enum.IsDefined(actorRole))
        {
            throw new ArgumentOutOfRangeException(nameof(actorRole), actorRole, "Business role is not defined.");
        }

        if (!Enum.IsDefined(decision))
        {
            throw new ArgumentOutOfRangeException(nameof(decision), decision, "Decision type is not defined.");
        }

        ActorRole = actorRole;
        Decision = decision;
        RequestAmount = NormalizeAmount(requestAmount, nameof(requestAmount));
        AvailableBudget = availableBudget.HasValue
            ? NormalizeAmount(availableBudget.Value, nameof(availableBudget))
            : null;
        OverBudgetAmount = NormalizeAmount(overBudgetAmount, nameof(overBudgetAmount));
        Reason = NormalizeReason(reason, decision);
        ValidatePeriod(budgetYear, budgetMonth);
        BudgetYear = budgetYear;
        BudgetMonth = budgetMonth;
        DecidedAt = NormalizeUtc(decidedAtUtc);
    }

    public Guid Id { get; private set; }
    public Guid PurchaseRequestId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public BusinessRole ActorRole { get; private set; }
    public PurchaseRequestDecisionType Decision { get; private set; }
    public decimal RequestAmount { get; private set; }
    public decimal? AvailableBudget { get; private set; }
    public decimal OverBudgetAmount { get; private set; }
    public string? Reason { get; private set; }
    public int? BudgetYear { get; private set; }
    public int? BudgetMonth { get; private set; }
    public DateTime DecidedAt { get; private set; }

    public static PurchaseRequestApprovalDecision Create(
        Guid purchaseRequestId,
        Guid actorUserId,
        BusinessRole actorRole,
        PurchaseRequestDecisionType decision,
        decimal requestAmount,
        decimal? availableBudget = null,
        decimal overBudgetAmount = 0m,
        string? reason = null,
        int? budgetYear = null,
        int? budgetMonth = null,
        DateTime? decidedAtUtc = null)
        => new(
            purchaseRequestId,
            actorUserId,
            actorRole,
            decision,
            requestAmount,
            availableBudget,
            overBudgetAmount,
            reason,
            budgetYear,
            budgetMonth,
            decidedAtUtc ?? DateTime.UtcNow);

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static decimal NormalizeAmount(decimal value, string parameterName)
    {
        if (value < 0 || value > BranchMonthlyBudget.MaximumAmount || decimal.Round(value, BranchMonthlyBudget.MoneyScale, MidpointRounding.AwayFromZero) != value)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Amount must be non-negative with at most two decimal places.");
        }

        return value;
    }

    private static string? NormalizeReason(string? reason, PurchaseRequestDecisionType decision)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            if (decision == PurchaseRequestDecisionType.Rejected)
            {
                throw new ArgumentException("A rejection reason is required.", nameof(reason));
            }

            return null;
        }

        var normalized = reason.Trim();
        if (normalized.Length > ReasonMaxLength)
        {
            throw new ArgumentException($"A decision reason cannot exceed {ReasonMaxLength} characters.", nameof(reason));
        }

        return normalized;
    }

    private static void ValidatePeriod(int? year, int? month)
    {
        if (year.HasValue != month.HasValue)
        {
            throw new ArgumentException("Budget year and month must be supplied together.");
        }

        if (month is not null && month is < BranchMonthlyBudget.MonthMinimum or > BranchMonthlyBudget.MonthMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }

        if (year is not null && year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be positive.");
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
