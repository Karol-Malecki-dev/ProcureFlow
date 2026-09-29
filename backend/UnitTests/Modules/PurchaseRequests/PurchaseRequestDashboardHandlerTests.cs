using Application.Modules.PurchaseRequests;
using Domain.Models.Organizations.Enums;
using Infrastructure.Modules.PurchaseRequests;
using Moq;

namespace UnitTests.Modules.PurchaseRequests;

public sealed class PurchaseRequestDashboardHandlerTests
{
    private readonly Mock<IPurchaseRequestMembershipReader> _membershipReader = new();
    private readonly Mock<IPurchaseRequestDashboardStore> _store = new();

    [Fact]
    public async Task Handle_uses_the_manager_branch_scope()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var membership = new PurchaseRequestMembership(
            Guid.NewGuid(),
            userId,
            organizationId,
            branchId,
            BusinessRole.Manager,
            true,
            true,
            true);
        var dashboard = CreateDashboard(BusinessRole.Manager);
        var query = new GetPurchaseRequestDashboardQuery(userId, organizationId);

        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                userId,
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
        _store
            .Setup(store => store.QueryAsync(
                It.Is<PurchaseRequestDashboardScope>(scope =>
                    scope.UserId == userId
                    && scope.OrganizationId == organizationId
                    && scope.BranchId == branchId
                    && scope.Role == BusinessRole.Manager
                    && !scope.IsOrganizationWide),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboard);

        var result = await CreateHandler().HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Same(dashboard, result.Value);
        _store.Verify(store => store.QueryAsync(
            It.IsAny<PurchaseRequestDashboardScope>(),
            It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc && value.Day == 1),
            It.Is<DateTime>(value => value.Kind == DateTimeKind.Utc),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_gives_platform_admin_an_organization_wide_scope()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var membership = new PurchaseRequestMembership(
            Guid.NewGuid(),
            userId,
            organizationId,
            Guid.NewGuid(),
            BusinessRole.Manager,
            true,
            true,
            false,
            IsPlatformAdmin: true);
        var query = new GetPurchaseRequestDashboardQuery(userId, organizationId);

        SetupMembership(query, membership);
        _store
            .Setup(store => store.QueryAsync(
                It.Is<PurchaseRequestDashboardScope>(scope =>
                    scope.IsOrganizationWide
                    && scope.BranchId == null
                    && scope.Role == BusinessRole.Manager),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDashboard(BusinessRole.Manager));

        var result = await CreateHandler().HandleAsync(query);

        Assert.True(result.IsSuccess);
        _store.Verify(store => store.QueryAsync(
            It.Is<PurchaseRequestDashboardScope>(scope => scope.IsOrganizationWide),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_an_inactive_manager_scope()
    {
        var query = new GetPurchaseRequestDashboardQuery(Guid.NewGuid(), Guid.NewGuid());
        SetupMembership(query, new PurchaseRequestMembership(
            Guid.NewGuid(),
            query.UserId,
            query.OrganizationId,
            Guid.NewGuid(),
            BusinessRole.Manager,
            true,
            true,
            false));

        var result = await CreateHandler().HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.QueryAsync(
            It.IsAny<PurchaseRequestDashboardScope>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private GetPurchaseRequestDashboardHandler CreateHandler()
        => new(_membershipReader.Object, _store.Object);

    private void SetupMembership(
        GetPurchaseRequestDashboardQuery query,
        PurchaseRequestMembership membership)
    {
        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                query.UserId,
                query.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private static PurchaseRequestDashboardView CreateDashboard(BusinessRole role)
        => new(
            role,
            2,
            21m,
            [new PurchaseRequestDashboardProductView(
                Guid.NewGuid(),
                "Monitor",
                "MON-1",
                2m,
                1)],
            [new PurchaseRequestDashboardBranchView(
                Guid.NewGuid(),
                "Main branch",
                21m)]);
}