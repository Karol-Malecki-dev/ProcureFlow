using Application.Modules.Organization.Branch.ArchiveBranch;
using Domain.ValueObjects;
using Infrastructure.Modules.Organization.Branch.ArchiveBranch;
using Moq;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace UnitTests.Modules.Organization.ArchiveBranch;

public sealed class ArchiveBranchHandlerTests
{
    private readonly Mock<IArchiveBranchStore> _store = new();

    [Fact]
    public async Task Handle_archives_branch_through_organization_aggregate()
    {
        var organization = CreateOrganizationWithBranch(out var branch);
        _store
            .Setup(store => store.GetOrganizationWithBranchesAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(
            new ArchiveBranchCommand(organization.Id, branch.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(ArchiveBranchStatus.Success, result.Status);
        Assert.True(branch.IsArchived);
        Assert.True(result.Value);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_organization_does_not_exist()
    {
        var organizationId = Guid.NewGuid();
        _store
            .Setup(store => store.GetOrganizationWithBranchesAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainOrganization?)null);

        var result = await CreateHandler().HandleAsync(
            new ArchiveBranchCommand(organizationId, Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ArchiveBranchStatus.NotFound, result.Status);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_branch_is_not_owned_by_organization()
    {
        var organization = CreateOrganizationWithBranch(out _);
        _store
            .Setup(store => store.GetOrganizationWithBranchesAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(
            new ArchiveBranchCommand(organization.Id, Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ArchiveBranchStatus.NotFound, result.Status);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_is_idempotent_for_already_archived_branch()
    {
        var organization = CreateOrganizationWithBranch(out var branch);
        branch.Archive();
        _store
            .Setup(store => store.GetOrganizationWithBranchesAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(
            new ArchiveBranchCommand(organization.Id, branch.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("Branch already archived", result.Message);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private ArchiveBranchHandler CreateHandler()
        => new(_store.Object);

    private static DomainOrganization CreateOrganizationWithBranch(out DomainBranch branch)
    {
        var organization = new DomainOrganization(
            "ProcureFlow",
            CreateAddress(),
            "PF",
            null);
        branch = new DomainBranch(
            "Warsaw",
            CreateAddress(),
            "WAW",
            organization.Id);
        organization.AddBranch(branch);
        return organization;
    }

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
