using Domain.Entities;
using Domain.Models.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Maps request items, historical snapshots, precision and duplicate-line protection.
/// </summary>
public sealed class PurchaseRequestItemConfiguration : IEntityTypeConfiguration<PurchaseRequestItem>
{
    public void Configure(EntityTypeBuilder<PurchaseRequestItem> builder)
    {
        builder.ToTable("PurchaseRequestItems", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseRequestItems_Quantity_Range",
                "\"Quantity\" > 0 AND \"Quantity\" <= 1000000");
            table.HasCheckConstraint(
                "CK_PurchaseRequestItems_UnitPrice_NonNegative",
                "\"UnitPriceSnapshot\" >= 0");
        });

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id)
            .ValueGeneratedNever();
        builder.Property(item => item.PurchaseRequestId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.ProductNameSnapshot)
            .IsRequired()
            .HasMaxLength(PurchaseRequestItem.ProductNameMaxLength);
        builder.Property(item => item.ProductCodeSnapshot)
            .HasMaxLength(PurchaseRequestItem.ProductCodeMaxLength);
        builder.Property(item => item.UnitNameSnapshot)
            .IsRequired()
            .HasMaxLength(PurchaseRequestItem.UnitNameMaxLength);
        builder.Property(item => item.UnitSymbolSnapshot)
            .IsRequired()
            .HasMaxLength(PurchaseRequestItem.UnitSymbolMaxLength);
        builder.Property(item => item.UnitPriceSnapshot)
            .HasPrecision(12, PurchaseRequestItem.MoneyScale)
            .IsRequired();
        builder.Property(item => item.Quantity)
            .HasPrecision(12, PurchaseRequestItem.QuantityScale)
            .IsRequired();
        builder.Property(item => item.Comment)
            .HasMaxLength(PurchaseRequestItem.CommentMaxLength);
        builder.Ignore(item => item.LineTotal);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => new { item.PurchaseRequestId, item.ProductId })
            .HasDatabaseName("UX_PurchaseRequestItems_Request_Product")
            .IsUnique();
        builder.HasIndex(item => item.ProductId);
    }
}
