using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.Budget;

/// <summary>Validates the budget limit and update concurrency token.</summary>
public sealed class UpsertBranchMonthlyBudgetRequestValidator
    : AbstractValidator<UpsertBranchMonthlyBudgetRequest>
{
    public UpsertBranchMonthlyBudgetRequestValidator()
    {
        RuleFor(request => request.LimitAmount)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(BranchMonthlyBudget.MaximumAmount)
            .Must(value => decimal.Round(
                value,
                BranchMonthlyBudget.MoneyScale,
                MidpointRounding.AwayFromZero) == value)
            .WithMessage("LimitAmount must have at most two decimal places.");

        RuleFor(request => request.ExpectedConcurrencyStamp)
            .MaximumLength(64)
            .When(request => request.ExpectedConcurrencyStamp is not null);
    }
}
