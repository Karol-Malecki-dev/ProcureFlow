using Domain.Models.Organizations;
using Domain.Models.Organizations.Enums;

namespace UnitTests.Domain.Models.Organizations;

public sealed class MembershipTests
{
    [Fact]
    public void Constructor_creates_active_branch_scoped_membership_for_manager()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var membership = new Membership(
            organizationId,
            userId,
            branchId,
            BusinessRole.Manager);

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(organizationId, membership.OrganizationId);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(branchId, membership.BranchId);
        Assert.Equal(BusinessRole.Manager, membership.Role);
        Assert.True(membership.IsActive);
    }

    [Fact]
    public void Constructor_creates_active_organization_scoped_membership_for_procurement()
    {
        var membership = new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            branchId: null,
            BusinessRole.Procurement);

        Assert.Null(membership.BranchId);
        Assert.Equal(BusinessRole.Procurement, membership.Role);
        Assert.True(membership.IsActive);
    }

    [Theory]
    [InlineData(BusinessRole.Employee)]
    [InlineData(BusinessRole.Manager)]
    public void Constructor_rejects_branch_scoped_role_without_branch(BusinessRole role)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            branchId: null,
            role));

        Assert.Contains("requires a branch", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_procurement_membership_with_branch()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessRole.Procurement));

        Assert.Contains("cannot have a branch", exception.Message);
    }

    [Fact]
    public void Constructor_rejects_undefined_business_role()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            branchId: null,
            (BusinessRole)999));

        Assert.Contains("not defined", exception.Message);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_rejects_empty_required_identifiers(
        bool emptyOrganizationId,
        bool emptyUserId)
    {
        var exception = Assert.Throws<ArgumentException>(() => new Membership(
            emptyOrganizationId ? Guid.Empty : Guid.NewGuid(),
            emptyUserId ? Guid.Empty : Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessRole.Employee));

        Assert.Contains("cannot be empty", exception.Message);
    }

    [Fact]
    public void Inactive_membership_cannot_be_updated_or_deactivated_again()
    {
        var membership = new Membership(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            BusinessRole.Employee);

        membership.Deactivate();

        Assert.False(membership.IsActive);
        Assert.Throws<InvalidOperationException>(() => membership.UpdateAssignment(
            BusinessRole.Manager,
            Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => membership.ChangeBranch(Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => membership.UpdateRole(BusinessRole.Manager));
        Assert.Throws<InvalidOperationException>(() => membership.Deactivate());
    }
}
