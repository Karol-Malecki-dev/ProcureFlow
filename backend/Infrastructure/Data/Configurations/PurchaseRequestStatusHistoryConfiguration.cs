using Domain.Entities;
using Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Maps immutable purchase-request lifecycle history entries.</summary>
public sealed class PurchaseRequestStatusHistoryConfiguration
    : IEntityTypeConfiguration<PurchaseRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestStatusHistory> builder)
    {
        builder.ToTable("PurchaseRequestStatusHistories");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.PurchaseRequestId).IsRequired();
        builder.Property(history => history.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(history => history.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(history => history.ChangedByUserId).IsRequired();
        builder.Property(history => history.ChangedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(history => history.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new
        {
            history.PurchaseRequestId,
            history.ChangedAt,
            history.Id
        });
    }
}