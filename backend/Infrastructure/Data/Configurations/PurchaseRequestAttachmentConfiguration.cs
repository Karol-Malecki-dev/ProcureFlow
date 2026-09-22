using Domain.Entities;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>Maps purchase-request attachment metadata and its ownership constraints.</summary>
public sealed class PurchaseRequestAttachmentConfiguration
    : IEntityTypeConfiguration<PurchaseRequestAttachment>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestAttachment> builder)
    {
        builder.ToTable("PurchaseRequestAttachments", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseRequestAttachments_SizeBytes_Positive",
                "\"SizeBytes\" > 0");
        });
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.OriginalFileName)
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(attachment => attachment.StoredFileName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(attachment => attachment.SizeBytes)
            .IsRequired();
        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<PurchaseRequest>()
            .WithMany()
            .HasForeignKey(attachment => attachment.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(attachment => new
        {
            attachment.PurchaseRequestId,
            attachment.CreatedAt
        });
    }
}
