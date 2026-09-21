using Domain.Entities;
using Domain.Models.Catalog;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Infrastructure.Data.Configurations.Catalog;

/// <summary>
/// Maps catalog products and protects the product-selection invariants in PostgreSQL.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.OrganizationId).IsRequired();
        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);
        builder.Property(product => product.Code)
            .HasMaxLength(Product.CodeMaxLength);
        builder.Property(product => product.UnitOfMeasureId).IsRequired();
        builder.Property(product => product.UnitPrice)
            .HasPrecision(12, Product.MoneyScale)
            .IsRequired();
        builder.Property(product => product.IsAvailable).IsRequired();
        builder.Property(product => product.IsActive).IsRequired();
        builder.Property(product => product.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(product => product.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(product => product.CreatedByUserId).IsRequired();
        builder.Property(product => product.UpdatedByUserId).IsRequired();

        builder.HasOne<DomainOrganization>()
            .WithMany()
            .HasForeignKey(product => product.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>()
            .WithMany()
            .HasForeignKey(product => product.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(product => product.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(product => product.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => new { product.OrganizationId, product.Code })
            .HasDatabaseName("UX_Products_Organization_Code")
            .HasFilter("\"Code\" IS NOT NULL")
            .IsUnique();
        builder.HasIndex(product => new
        {
            product.OrganizationId,
            product.IsActive,
            product.IsAvailable
        });
    }
}
