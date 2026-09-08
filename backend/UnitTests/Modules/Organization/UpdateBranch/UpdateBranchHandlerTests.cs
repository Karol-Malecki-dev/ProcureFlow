using Application.Modules.Organization.UpdateBranch;
using Domain.ValueObjects;
using Infrastructure.Modules.Organization.UpdateBranch;
using Moq;
using DomainBranch = Domain.Models.Organizations.Organization.Branch;

namespace UnitTests.Modules.Organization.UpdateBranch;

public sealed class UpdateBranchHandlerTests
{
    private readonly Mock<IUpdateBranchStore> _store = new();

    [Fact]
    public async Task Handle_updates_branch_when_name_and_code_are_available()
    {
        var organizationId = Guid.NewGuid();
        var branch = CreateBranch(organizationId);
        var command = new UpdateBranchCommand(
            organizationId,
            branch.Id,
            "Updated branch",
            "UPD",
            CreateAddress("Updated Street"));

        SetupBranch(branch, command);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(UpdateBranchStatus.Success, result.Status);
        Assert.Equal("Updated branch", result.Value!.Name);
        Assert.Equal("UPD", result.Value.Code);
        Assert.Equal("Updated Street", result.Value.Address.Street);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_not_found_for_branch_owned_by_another_organization()
    {
        var command = new UpdateBranchCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Updated branch",
            "UPD",
            CreateAddress());

        _store
            .Setup(store => store.GetOwnedBranchAsync(
                command.OrganizationId,
                command.BranchId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainBranch?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateBranchStatus.NotFound, result.Status);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_archived_branch()
    {
        var organizationId = Guid.NewGuid();
        var branch = CreateBranch(organizationId);
        branch.Archive();
        var command = new UpdateBranchCommand(
            organizationId,
            branch.Id,
            "Updated branch",
            "UPD",
            CreateAddress());

        SetupBranch(branch, command, setupUniqueness: false);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateBranchStatus.Conflict, result.Status);
        _store.Verify(store => store.CodeExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_duplicate_code()
    {
        var organizationId = Guid.NewGuid();
        var branch = CreateBranch(organizationId);
        var command = new UpdateBranchCommand(
            organizationId,
            branch.Id,
            "Updated branch",
            "USED",
            CreateAddress());

        SetupBranch(branch, command);
        _store
            .Setup(store => store.CodeExistsAsync(
                command.OrganizationId,
                command.BranchId,
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateBranchStatus.Conflict, result.Status);
        _store.Verify(store => store.NameExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_duplicate_name()
    {
        var organizationId = Guid.NewGuid();
        var branch = CreateBranch(organizationId);
        var command = new UpdateBranchCommand(
            organizationId,
            branch.Id,
            "Used branch",
            "UPD",
            CreateAddress());

        SetupBranch(branch, command);
        _store
            .Setup(store => store.NameExistsAsync(
                command.OrganizationId,
                command.BranchId,
                command.Name,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UpdateBranchStatus.Conflict, result.Status);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupBranch(
        DomainBranch branch,
        UpdateBranchCommand command,
        bool setupUniqueness = true)
    {
        _store
            .Setup(store => store.GetOwnedBranchAsync(
                command.OrganizationId,
                command.BranchId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);

        if (setupUniqueness)
        {
            _store
                .Setup(store => store.CodeExistsAsync(
                    command.OrganizationId,
                    command.BranchId,
                    command.Code,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            _store
                .Setup(store => store.NameExistsAsync(
                    command.OrganizationId,
                    command.BranchId,
                    command.Name,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }
    }

    private UpdateBranchHandler CreateHandler()
        => new(_store.Object);

    private static DomainBranch CreateBranch(Guid organizationId)
        => new("Original branch", CreateAddress(), "ORG", organizationId);

    private static Address CreateAddress(string street = "Main Street")
        => new()
        {
            Street = street,
            BuildingNumber = "1",
            City = "Warsaw",
            PostalCode = "00-001",
            Country = "Poland"
        };
}
