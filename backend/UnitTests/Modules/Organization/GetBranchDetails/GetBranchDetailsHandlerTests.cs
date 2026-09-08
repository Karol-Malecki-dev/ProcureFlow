using Application.Modules.Organization.GetBranchDetails;
using Domain.ValueObjects;
using Infrastructure.Modules.Organization.GetBranchDetails;
using Moq;

namespace UnitTests.Modules.Organization.GetBranchDetails;

public sealed class GetBranchDetailsHandlerTests
{
    private readonly Mock<IGetBranchDetailsStore> _store = new();

    [Fact]
    public async Task Handle_returns_branch_details_when_branch_belongs_to_organization()
    {
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var branch = new BranchDetails(
            branchId,
            "Warsaw",
            "WAW",
            false,
            CreateAddress());

        _store
            .Setup(store => store.QueryAsync(
                It.Is<GetBranchDetailsQuery>(query =>
                    query.OrganizationId == organizationId
                    && query.BranchId == branchId
                    && !query.IncludeArchived),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(branch);

        var result = await CreateHandler().HandleAsync(
            new GetBranchDetailsQuery(organizationId, branchId));

        Assert.True(result.IsSuccess);
        Assert.Equal(GetBranchDetailsStatus.Success, result.Status);
        Assert.Equal(branch, result.Value);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_branch_is_not_in_requested_organization()
    {
        var query = new GetBranchDetailsQuery(Guid.NewGuid(), Guid.NewGuid());

        _store
            .Setup(store => store.QueryAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BranchDetails?)null);

        var result = await CreateHandler().HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(GetBranchDetailsStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Handle_forwards_include_archived_to_store()
    {
        var query = new GetBranchDetailsQuery(Guid.NewGuid(), Guid.NewGuid(), IncludeArchived: true);

        _store
            .Setup(store => store.QueryAsync(
                It.Is<GetBranchDetailsQuery>(candidate => candidate.IncludeArchived),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BranchDetails(
                query.BranchId,
                "Archived branch",
                "ARC",
                true,
                CreateAddress()));

        var result = await CreateHandler().HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsArchived);
        _store.Verify(store => store.QueryAsync(
            It.Is<GetBranchDetailsQuery>(candidate => candidate.IncludeArchived),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private GetBranchDetailsHandler CreateHandler()
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
