
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Infrastructure.Data.Configurations.Organization;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<DomainOrganization>
{
    public void Configure(EntityTypeBuilder<DomainOrganization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(organization => organization.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(organization => organization.Bio)
            .HasMaxLength(2000);

        builder.Property(organization => organization.IsArchived)
            .IsRequired();

        builder.HasIndex(organization => organization.Code)
            .IsUnique();

        builder.HasIndex(organization => organization.IsArchived)
            .HasDatabaseName("UX_Organizations_Active")
            .HasFilter("\"IsArchived\" = false")
            .IsUnique();

        builder.HasMany(organization => organization.Branches)
            .WithOne()
            .HasForeignKey(branch => branch.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(organization => organization.Branches)
            .HasField("_branches")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsOne(
            organization => organization.Address,
            address => ConfigureAddress(address));
    }

    private static void ConfigureAddress(
        OwnedNavigationBuilder<DomainOrganization, Domain.ValueObjects.Address> address)
    {
        address.Property(value => value.Street)
            .HasColumnName("Address_Street")
            .IsRequired()
            .HasMaxLength(200);

        address.Property(value => value.BuildingNumber)
            .HasColumnName("Address_BuildingNumber")
            .IsRequired()
            .HasMaxLength(30);

        address.Property(value => value.ApartmentNumber)
            .HasColumnName("Address_ApartmentNumber")
            .HasMaxLength(30);

        address.Property(value => value.City)
            .HasColumnName("Address_City")
            .IsRequired()
            .HasMaxLength(100);

        address.Property(value => value.PostalCode)
            .HasColumnName("Address_PostalCode")
            .IsRequired()
            .HasMaxLength(20);

        address.Property(value => value.Country)
            .HasColumnName("Address_Country")
            .IsRequired()
            .HasMaxLength(100);
    }
}
