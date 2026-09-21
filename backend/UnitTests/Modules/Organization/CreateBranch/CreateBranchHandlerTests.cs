using DomainBranch = Domain.Models.Organizations.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization;
using Domain.Models.Organizations.Enums;
using Domain.ValueObjects;
using Moq;
using Application.Modules.Organization.Branch.CreateBranch;
using Infrastructure.Modules.Organization.Branch.CreateBranch;

namespace UnitTests.Modules.Organization.CreateBranch;

public sealed class CreateBranchHandlerTests
{
    private readonly Mock<ICreateBranchStore> _store = new();

    [Fact]
    public async Task Handle_creates_branch_for_existing_organization()
    {
        var organization = CreateOrganization();
        var command = new CreateBranchCommand(
            organization.Id,
            "Warsaw branch",
            " waw ",
            CreateAddress());

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.CodeExistsAsync(
                organization.Id,
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _store
            .Setup(store => store.NameExistsAsync(
                organization.Id,
                command.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(BranchOperationStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("WAW", result.Value!.Code);
        Assert.Contains(organization.Branches, branch => branch.Id == result.Value.Id);

        _store.Verify(store => store.Add(It.Is<DomainBranch>(branch =>
            branch.OrganizationId == organization.Id
            && branch.Name == "Warsaw branch"
            && branch.Code == "WAW")), Times.Once);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_organization_does_not_exist()
    {
        var organizationId = Guid.NewGuid();
        var command = new CreateBranchCommand(
            organizationId,
            "Warsaw branch",
            "WAW",
            CreateAddress());

        _store
            .Setup(store => store.GetOrganizationAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainOrganization?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(BranchOperationStatus.NotFound, result.Status);
        _store.Verify(store => store.CodeExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.NameExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainBranch>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_code_is_already_used()
    {
        var organization = CreateOrganization();
        var command = new CreateBranchCommand(
            organization.Id,
            "Warsaw branch",
            "WAW",
            CreateAddress());

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.CodeExistsAsync(
                organization.Id,
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(BranchOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainBranch>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_name_is_already_used()
    {
        var organization = CreateOrganization();
        var command = new CreateBranchCommand(
            organization.Id,
            "Warsaw branch",
            "WAW",
            CreateAddress());

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.CodeExistsAsync(
                organization.Id,
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _store
            .Setup(store => store.NameExistsAsync(
                organization.Id,
                command.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(BranchOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainBranch>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_rejects_archived_organization()
    {
        var organization = CreateOrganization();
        organization.Archive();

        var command = new CreateBranchCommand(
            organization.Id,
            "Warsaw branch",
            "WAW",
            CreateAddress());

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(BranchOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.CodeExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.NameExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainBranch>()), Times.Never);
    }

    private CreateBranchHandler CreateHandler()
        => new(_store.Object);

    private static DomainOrganization CreateOrganization()
        => new(
            "ProcureFlow",
            CreateAddress(),
            "PF",
            "Organization used by the test.");

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