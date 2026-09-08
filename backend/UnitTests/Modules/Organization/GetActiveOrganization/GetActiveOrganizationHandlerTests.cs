using Application.Modules.Organization.GetActiveOrganization;
using Infrastructure.Modules.Organization.GetActiveOrganization;
using Moq;

namespace UnitTests.Modules.Organization.GetActiveOrganization;

public sealed class GetActiveOrganizationHandlerTests
{
    private readonly Mock<IGetActiveOrganizationStore> _store = new();

    [Fact]
    public async Task Handle_returns_the_active_organization()
    {
        var organization = new ActiveOrganization(
            Guid.NewGuid(),
            "ProcureFlow MVP",
            "PF-MVP");
        _store
            .Setup(store => store.QueryAsync(
                It.IsAny<GetActiveOrganizationQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(new GetActiveOrganizationQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(GetActiveOrganizationStatus.Success, result.Status);
        Assert.Equal(organization, result.Value);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_no_active_organization_exists()
    {
        _store
            .Setup(store => store.QueryAsync(
                It.IsAny<GetActiveOrganizationQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveOrganization?)null);

        var result = await CreateHandler().HandleAsync(new GetActiveOrganizationQuery());

        Assert.False(result.IsSuccess);
        Assert.Equal(GetActiveOrganizationStatus.NotFound, result.Status);
        Assert.Null(result.Value);
    }

    private GetActiveOrganizationHandler CreateHandler()
        => new(_store.Object);
}
