using Domain.Entities;
using Domain.Enums;
using Domain.Models.Organizations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Infrastructure.Data.Configurations;

/// <summary>
/// Maps the purchase-request aggregate root and its scope/concurrency invariants.
/// </summary>
public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests", table =>
        {
            table.HasCheckConstraint(
                "CK_PurchaseRequests_TotalValue_NonNegative",
                "\"TotalValue\" >= 0");
        });

        builder.HasKey(request => request.Id);
        builder.Property(request => request.AuthorUserId).IsRequired();
        builder.Property(request => request.OrganizationId).IsRequired();
        builder.Property(request => request.BranchId).IsRequired();
        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(request => request.Note)
            .HasMaxLength(PurchaseRequest.NoteMaxLength);
        builder.Property(request => request.TotalValue)
            .HasPrecision(12, PurchaseRequestItem.MoneyScale)
            .IsRequired();
        builder.Property(request => request.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(request => request.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(request => request.ConcurrencyStamp)
            .IsRequired()
            .HasMaxLength(64)
            .IsConcurrencyToken();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DomainOrganization>()
            .WithMany()
            .HasForeignKey(request => request.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(request => request.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(request => request.Items)
            .WithOne()
            .HasForeignKey(item => item.PurchaseRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(request => request.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(request => new
        {
            request.AuthorUserId,
            request.OrganizationId,
            request.BranchId,
            request.UpdatedAt,
            request.Id
        });
        builder.HasIndex(request => new { request.OrganizationId, request.Status });
    }
}
