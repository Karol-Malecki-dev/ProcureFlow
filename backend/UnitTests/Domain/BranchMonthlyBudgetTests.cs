using Domain.Entities;

namespace UnitTests.Domain;

public sealed class BranchMonthlyBudgetTests
{
    [Fact]
    public void Create_starts_with_full_available_amount()
    {
        var budget = BranchMonthlyBudget.Create(Guid.NewGuid(), 2026, 9, 1_000m);

        Assert.Equal(1_000m, budget.LimitAmount);
        Assert.Equal(0m, budget.UsedAmount);
        Assert.Equal(1_000m, budget.AvailableAmount);
        Assert.False(string.IsNullOrWhiteSpace(budget.ConcurrencyStamp));
    }

    [Fact]
    public void Reserve_updates_used_and_available_amount()
    {
        var budget = BranchMonthlyBudget.Create(Guid.NewGuid(), 2026, 9, 1_000m);
        var initialStamp = budget.ConcurrencyStamp;

        budget.Reserve(125.50m);

        Assert.Equal(125.50m, budget.UsedAmount);
        Assert.Equal(874.50m, budget.AvailableAmount);
        Assert.NotEqual(initialStamp, budget.ConcurrencyStamp);
    }

    [Fact]
    public void Reserve_rejects_amount_above_available_budget()
    {
        var budget = BranchMonthlyBudget.Create(Guid.NewGuid(), 2026, 9, 100m);

        Assert.Throws<InvalidOperationException>(() => budget.Reserve(100.01m));
        Assert.Equal(0m, budget.UsedAmount);
    }

    [Fact]
    public void Reserve_over_limit_allows_procurement_override()
    {
        var budget = BranchMonthlyBudget.Create(Guid.NewGuid(), 2026, 9, 100m);

        budget.ReserveOverLimit(125m);

        Assert.Equal(125m, budget.UsedAmount);
        Assert.Equal(-25m, budget.AvailableAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Create_rejects_invalid_month(int month)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BranchMonthlyBudget.Create(Guid.NewGuid(), 2026, month, 100m));
    }
}
