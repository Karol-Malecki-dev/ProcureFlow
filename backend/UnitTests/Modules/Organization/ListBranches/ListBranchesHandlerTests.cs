using Application.Modules.Organization.Branch.ListBranches;
using Domain.ValueObjects;
using Infrastructure.Modules.Organization.Branch.ListBranches;
using Moq;

namespace UnitTests.Modules.Organization.ListBranches;

public sealed class ListBranchesHandlerTests
{
    private readonly Mock<IListBranchesStore> _store = new();

    [Fact]
    public async Task Handle_returns_branches_for_existing_organization()
    {
        var organizationId = Guid.NewGuid();
        var branches = new List<BranchListItem>
        {
            new(
                Guid.NewGuid(),
                "Warsaw",
                "WAW",
                false,
                CreateAddress())
        };

        _store
            .Setup(store => store.OrganizationExistsAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _store
            .Setup(store => store.QueryAsync(
                It.Is<ListBranchesQuery>(query =>
                    query.OrganizationId == organizationId
                    && !query.IncludeArchived),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(branches);

        var result = await CreateHandler().HandleAsync(
            new ListBranchesQuery(organizationId));

        Assert.True(result.IsSuccess);
        Assert.Equal(ListBranchesStatus.Success, result.Status);
        Assert.Single(result.Value!);
        Assert.Equal("WAW", result.Value![0].Code);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_organization_does_not_exist()
    {
        var organizationId = Guid.NewGuid();

        _store
            .Setup(store => store.OrganizationExistsAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().HandleAsync(
            new ListBranchesQuery(organizationId));

        Assert.False(result.IsSuccess);
        Assert.Equal(ListBranchesStatus.NotFound, result.Status);
        Assert.Null(result.Value);
        _store.Verify(store => store.QueryAsync(
            It.IsAny<ListBranchesQuery>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_forwards_include_archived_to_store()
    {
        var organizationId = Guid.NewGuid();

        _store
            .Setup(store => store.OrganizationExistsAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _store
            .Setup(store => store.QueryAsync(
                It.Is<ListBranchesQuery>(query =>
                    query.OrganizationId == organizationId
                    && query.IncludeArchived),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BranchListItem>());

        var result = await CreateHandler().HandleAsync(
            new ListBranchesQuery(organizationId, IncludeArchived: true));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
        _store.Verify(store => store.QueryAsync(
            It.Is<ListBranchesQuery>(query => query.IncludeArchived),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private ListBranchesHandler CreateHandler()
        => new(_store.Object);

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
