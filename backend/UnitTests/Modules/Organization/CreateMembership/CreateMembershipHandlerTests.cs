using Application.Modules.Organization.Membership.Create;
using Domain.Entities;
using Domain.Models.Organizations.Enums;
using Domain.ValueObjects;
using Infrastructure.Modules.Organization.Membership.Create;
using Moq;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace UnitTests.Modules.Organization.CreateMembership;

public sealed class CreateMembershipHandlerTests
{
    private readonly Mock<ICreateMembershipStore> _store = new();

    [Fact]
    public async Task Handle_creates_active_manager_membership_for_active_branch()
    {
        var organization = CreateOrganization();
        var user = CreateUser();
        var branch = CreateBranch(organization.Id);
        var command = new CreateMembershipCommand(
            organization.Id,
            user.Id,
            branch.Id,
            BusinessRole.Manager);

        SetupActiveAssignment(command, organization, user, branch);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(organization.Id, result.Value!.OrganizationId);
        Assert.Equal(user.Id, result.Value.UserId);
        Assert.Equal(branch.Id, result.Value.BranchId);
        Assert.Equal(BusinessRole.Manager, result.Value.Role);
        Assert.True(result.Value.IsActive);
        _store.Verify(store => store.Add(It.Is<DomainMembership>(membership =>
            membership.OrganizationId == organization.Id
            && membership.UserId == user.Id
            && membership.BranchId == branch.Id
            && membership.Role == BusinessRole.Manager
            && membership.IsActive)), Times.Once);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_creates_organization_scoped_procurement_membership_without_branch()
    {
        var organization = CreateOrganization();
        var user = CreateUser();
        var command = new CreateMembershipCommand(
            organization.Id,
            user.Id,
            BranchId: null,
            BusinessRole.Procurement);

        SetupActiveAssignment(command, organization, user);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.BranchId);
        Assert.Equal(BusinessRole.Procurement, result.Value.Role);
        _store.Verify(store => store.GetActiveBranchAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_branch_is_outside_organization_scope()
    {
        var organization = CreateOrganization();
        var user = CreateUser();
        var foreignOrganization = CreateOrganization();
        var foreignBranch = CreateBranch(foreignOrganization.Id);
        var command = new CreateMembershipCommand(
            organization.Id,
            user.Id,
            foreignBranch.Id,
            BusinessRole.Employee);

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.GetActiveUserAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _store
            .Setup(store => store.ActiveMembershipExistsAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _store
            .Setup(store => store.GetActiveBranchAsync(
                organization.Id,
                foreignBranch.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBranch?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.NotFound, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainMembership>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_archived_organization()
    {
        var organization = CreateOrganization();
        organization.Archive();
        var command = new CreateMembershipCommand(
            organization.Id,
            Guid.NewGuid(),
            BranchId: null,
            BusinessRole.Procurement);

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.GetActiveUserAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_inactive_user()
    {
        var organization = CreateOrganization();
        var userId = Guid.NewGuid();
        var command = new CreateMembershipCommand(
            organization.Id,
            userId,
            BranchId: null,
            BusinessRole.Procurement);

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.GetActiveUserAsync(
                userId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.NotFound, result.Status);
        _store.Verify(store => store.ActiveMembershipExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_user_already_has_active_membership()
    {
        var organization = CreateOrganization();
        var user = CreateUser();
        var command = new CreateMembershipCommand(
            organization.Id,
            user.Id,
            BranchId: null,
            BusinessRole.Procurement);

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.GetActiveUserAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _store
            .Setup(store => store.ActiveMembershipExistsAsync(
                user.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainMembership>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_validation_error_for_branchless_employee()
    {
        var organization = CreateOrganization();
        var user = CreateUser();
        var command = new CreateMembershipCommand(
            organization.Id,
            user.Id,
            BranchId: null,
            BusinessRole.Employee);

        SetupActiveAssignment(command, organization, user);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MembershipOperationStatus.ValidationError, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainMembership>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupActiveAssignment(
        CreateMembershipCommand command,
        DomainOrganization organization,
        User user,
        DomainBranch? branch = null)
    {
        _store
            .Setup(store => store.GetOrganizationAsync(
                command.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.GetActiveUserAsync(
                command.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _store
            .Setup(store => store.ActiveMembershipExistsAsync(
                command.UserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        if (branch is not null)
        {
            _store
                .Setup(store => store.GetActiveBranchAsync(
                    command.OrganizationId,
                    branch.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(branch);
        }

        _store
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateMembershipHandler CreateHandler()
        => new(_store.Object);

    private static DomainOrganization CreateOrganization()
        => new(
            "ProcureFlow",
            CreateAddress(),
            $"PF-{Guid.NewGuid():N}",
            null);

    private static DomainBranch CreateBranch(Guid organizationId)
        => new("Warsaw branch", CreateAddress(), $"BR-{Guid.NewGuid():N}", organizationId);

    private static User CreateUser()
        => User.Create(
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            DisplayName.Create("Test user"));

    private static Address CreateAddress()
        => new()
        {
            Street = "Main Street",
            BuildingNumber = "1",
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };
}