using Domain.Entities;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Maps branch-owned monthly budgets and their optimistic concurrency token.</summary>
public sealed class BranchMonthlyBudgetConfiguration : IEntityTypeConfiguration<BranchMonthlyBudget>
{
    public void Configure(EntityTypeBuilder<BranchMonthlyBudget> builder)
    {
        builder.ToTable("BranchMonthlyBudgets", table =>
        {
            table.HasCheckConstraint(
                "CK_BranchMonthlyBudgets_LimitAmount_NonNegative",
                "\"LimitAmount\" >= 0");
            table.HasCheckConstraint(
                "CK_BranchMonthlyBudgets_UsedAmount_NonNegative",
                "\"UsedAmount\" >= 0");
            table.HasCheckConstraint(
                "CK_BranchMonthlyBudgets_Month_Range",
                "\"Month\" >= 1 AND \"Month\" <= 12");
        });

        builder.HasKey(budget => budget.Id);
        builder.Property(budget => budget.BranchId).IsRequired();
        builder.Property(budget => budget.Year).IsRequired();
        builder.Property(budget => budget.Month).IsRequired();
        builder.Property(budget => budget.LimitAmount)
            .HasPrecision(14, BranchMonthlyBudget.MoneyScale)
            .IsRequired();
        builder.Property(budget => budget.UsedAmount)
            .HasPrecision(14, BranchMonthlyBudget.MoneyScale)
            .IsRequired();
        builder.Property(budget => budget.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(budget => budget.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(budget => new
        {
            budget.BranchId,
            budget.Year,
            budget.Month
        })
        .HasDatabaseName("UX_BranchMonthlyBudgets_Branch_Period")
        .IsUnique();
    }
}