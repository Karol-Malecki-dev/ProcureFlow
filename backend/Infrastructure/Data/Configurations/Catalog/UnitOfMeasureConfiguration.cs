using Domain.Entities;
using Domain.Models.Catalog;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Infrastructure.Data.Configurations.Catalog;

/// <summary>
/// Maps organization-owned unit-of-measure reference data and its database invariants.
/// </summary>
public sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitOfMeasures");
        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.OrganizationId)
            .IsRequired();

        builder.Property(unit => unit.Name)
            .IsRequired()
            .HasMaxLength(UnitOfMeasure.NameMaxLength);

        builder.Property(unit => unit.Symbol)
            .IsRequired()
            .HasMaxLength(UnitOfMeasure.SymbolMaxLength);

        builder.Property(unit => unit.IsActive)
            .IsRequired();

        builder.Property(unit => unit.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(unit => unit.UpdatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(unit => unit.CreatedByUserId)
            .IsRequired();

        builder.Property(unit => unit.UpdatedByUserId)
            .IsRequired();

        builder.HasOne<DomainOrganization>()
            .WithMany()
            .HasForeignKey(unit => unit.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(unit => unit.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(unit => unit.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(unit => new
        {
            unit.OrganizationId,
            unit.Symbol
        })
        .HasDatabaseName("UX_UnitOfMeasures_Organization_ActiveSymbol")
        .HasFilter("\"IsActive\" = true")
        .IsUnique();

        builder.HasIndex(unit => new
        {
            unit.OrganizationId,
            unit.IsActive
        });
    }
}