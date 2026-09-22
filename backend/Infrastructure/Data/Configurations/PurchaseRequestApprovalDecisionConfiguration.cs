using Domain.Entities;
using Domain.Entities.Auth;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Maps immutable approval outcomes and their budget context.</summary>
public sealed class PurchaseRequestApprovalDecisionConfiguration
    : IEntityTypeConfiguration<PurchaseRequestApprovalDecision>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestApprovalDecision> builder)
    {
        builder.ToTable("PurchaseRequestApprovalDecisions", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseRequestApprovalDecisions_RequestAmount_NonNegative",
                "\"RequestAmount\" >= 0");
            table.HasCheckConstraint(
                "CK_PurchaseRequestApprovalDecisions_OverBudgetAmount_NonNegative",
                "\"OverBudgetAmount\" >= 0");
        });

        builder.HasKey(decision => decision.Id);
        builder.Property(decision => decision.PurchaseRequestId).IsRequired();
        builder.Property(decision => decision.ActorUserId).IsRequired();
        builder.Property(decision => decision.ActorRole)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(decision => decision.Decision)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(decision => decision.RequestAmount)
            .HasPrecision(14, BranchMonthlyBudget.MoneyScale)
            .IsRequired();
        builder.Property(decision => decision.AvailableBudget)
            .HasPrecision(14, BranchMonthlyBudget.MoneyScale);
        builder.Property(decision => decision.OverBudgetAmount)
            .HasPrecision(14, BranchMonthlyBudget.MoneyScale)
            .IsRequired();
        builder.Property(decision => decision.Reason)
            .HasMaxLength(PurchaseRequestApprovalDecision.ReasonMaxLength);
        builder.Property(decision => decision.BudgetYear);
        builder.Property(decision => decision.BudgetMonth);
        builder.Property(decision => decision.DecidedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(decision => decision.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(decision => decision.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(decision => new
        {
            decision.PurchaseRequestId,
            decision.ActorRole
        })
        .HasDatabaseName("UX_PurchaseRequestApprovalDecisions_Request_Role")
        .IsUnique();
        builder.HasIndex(decision => new
        {
            decision.PurchaseRequestId,
            decision.DecidedAt,
            decision.Id
        });
    }
}