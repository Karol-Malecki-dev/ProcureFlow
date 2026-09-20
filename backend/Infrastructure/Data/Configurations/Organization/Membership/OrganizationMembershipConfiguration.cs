
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;
using DomainUser = Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations.Organization.Membership;

public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<DomainMembership>
{
    public void Configure(EntityTypeBuilder<DomainMembership> builder)
    {
        builder.ToTable("Memberships");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.OrganizationId)
            .IsRequired();
        builder.Property(m => m.UserId)
            .IsRequired();
        builder.Property(m => m.BranchId)
            .IsRequired(false);
        builder.Property(m => m.Role)
            .IsRequired();
        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.HasOne<DomainOrganization>()
            .WithMany()
            .HasForeignKey(membership => membership.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DomainUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<DomainBranch>()
            .WithMany()
            .HasForeignKey(membership => membership.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName("UX_Memberships_ActiveUser")
            .HasFilter("\"IsActive\" = true")
            .IsUnique();

        builder.HasIndex(membership => new
        {
            membership.OrganizationId,
            membership.IsActive,
            membership.BranchId
        });
    }
}
