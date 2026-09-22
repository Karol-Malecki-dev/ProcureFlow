using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Maps durable purchase-request attachment cleanup messages.</summary>
public sealed class PurchaseRequestAttachmentCleanupMessageConfiguration
    : IEntityTypeConfiguration<PurchaseRequestAttachmentCleanupMessage>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestAttachmentCleanupMessage> builder)
    {
        builder.ToTable("PurchaseRequestAttachmentCleanupMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.StoredFileName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(message => message.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(message => message.NextAttemptAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(message => message.ProcessedAt)
            .HasColumnType("timestamp with time zone");
        builder.Property(message => message.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.NextAttemptAt
        });
    }
}
