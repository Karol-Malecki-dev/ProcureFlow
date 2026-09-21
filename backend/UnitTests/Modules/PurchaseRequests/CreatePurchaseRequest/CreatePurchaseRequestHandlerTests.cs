using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.CreatePurchaseRequest;
using Domain.Models.Organizations.Enums;
using Infrastructure.Modules.PurchaseRequests.CreatePurchaseRequest;
using Moq;
using DomainPurchaseRequest = global::Domain.Entities.PurchaseRequest;
using DomainPurchaseRequestStatus = global::Domain.Enums.PurchaseRequestStatus;

namespace UnitTests.Modules.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestHandlerTests
{
    private readonly Mock<IPurchaseRequestMembershipReader> _membershipReader = new();
    private readonly Mock<ICreatePurchaseRequestStore> _store = new();

    [Fact]
    public async Task Handle_creates_empty_draft_from_active_employee_membership()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var command = new CreatePurchaseRequestCommand(
            userId,
            organizationId,
            " Need office supplies ");
        var membership = CreateMembership(userId, organizationId, branchId);

        SetupMembership(command, membership);
        _store
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(userId, result.Value!.AuthorUserId);
        Assert.Equal(organizationId, result.Value.OrganizationId);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Equal("Need office supplies", result.Value.Note);
        Assert.Equal(DomainPurchaseRequestStatus.Draft, result.Value.Status);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalValue);
        Assert.NotEmpty(result.Value.ConcurrencyStamp);
        _store.Verify(store => store.Add(It.Is<DomainPurchaseRequest>(request =>
            request.AuthorUserId == userId
            && request.OrganizationId == organizationId
            && request.BranchId == branchId
            && request.Status == DomainPurchaseRequestStatus.Draft
            && request.Items.Count == 0)), Times.Once);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_forbidden_for_non_employee_membership()
    {
        var command = new CreatePurchaseRequestCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);
        var membership = new PurchaseRequestMembership(
            command.UserId,
            command.UserId,
            command.OrganizationId,
            null,
            BusinessRole.Procurement,
            true,
            true,
            true);

        SetupMembership(command, membership);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Forbidden, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainPurchaseRequest>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_active_membership_is_missing()
    {
        var command = new CreatePurchaseRequestCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);
        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                command.UserId,
                command.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseRequestMembership?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.NotFound, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainPurchaseRequest>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_employee_scope_is_inactive()
    {
        var command = new CreatePurchaseRequestCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null);
        var membership = new PurchaseRequestMembership(
            Guid.NewGuid(),
            command.UserId,
            command.OrganizationId,
            Guid.NewGuid(),
            BusinessRole.Employee,
            true,
            true,
            false);

        SetupMembership(command, membership);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainPurchaseRequest>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_validation_error_before_resolving_membership_for_empty_identifier()
    {
        var command = new CreatePurchaseRequestCommand(
            Guid.Empty,
            Guid.NewGuid(),
            null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.ValidationError, result.Status);
        _membershipReader.Verify(reader => reader.GetCurrentMembershipAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainPurchaseRequest>()), Times.Never);
    }

    private CreatePurchaseRequestHandler CreateHandler()
        => new(_membershipReader.Object, _store.Object);

    private void SetupMembership(
        CreatePurchaseRequestCommand command,
        PurchaseRequestMembership membership)
    {
        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                command.UserId,
                command.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);
    }

    private static PurchaseRequestMembership CreateMembership(
        Guid userId,
        Guid organizationId,
        Guid branchId)
        => new(
            Guid.NewGuid(),
            userId,
            organizationId,
            branchId,
            BusinessRole.Employee,
            true,
            true,
            true);
}
