using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Organization;

public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(branch => branch.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(branch => branch.IsArchived)
            .IsRequired();

        builder.HasIndex(branch => new
        {
            branch.OrganizationId,
            branch.Code
        })
        .IsUnique();

        builder.HasIndex(branch => new
        {
            branch.OrganizationId,
            branch.Name
        })
        .IsUnique();

        builder.OwnsOne(
            branch => branch.Address,
            address =>
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
            });
    }
}