using Domain.Entities;
using Domain.Enums;
using Domain.Models.Organizations.Enums;

namespace UnitTests.Domain;

public sealed class PurchaseRequestApprovalDecisionTests
{
    [Fact]
    public void Rejected_decision_requires_a_reason()
    {
        Assert.Throws<ArgumentException>(() => PurchaseRequestApprovalDecision.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessRole.Manager,
            PurchaseRequestDecisionType.Rejected,
            100m));
    }

    [Fact]
    public void Escalated_decision_captures_budget_context()
    {
        var decision = PurchaseRequestApprovalDecision.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessRole.Manager,
            PurchaseRequestDecisionType.Escalated,
            125m,
            availableBudget: 100m,
            overBudgetAmount: 25m,
            budgetYear: 2026,
            budgetMonth: 9);

        Assert.Equal(125m, decision.RequestAmount);
        Assert.Equal(100m, decision.AvailableBudget);
        Assert.Equal(25m, decision.OverBudgetAmount);
        Assert.Equal(2026, decision.BudgetYear);
        Assert.Equal(9, decision.BudgetMonth);
    }
}
