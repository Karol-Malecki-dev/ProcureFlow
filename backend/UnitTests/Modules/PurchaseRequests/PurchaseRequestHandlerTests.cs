using Application.Modules.Catalog.ProductRead;
using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Application.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Application.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Domain.Models.Organizations.Enums;
using Infrastructure.Modules.PurchaseRequests.AddPurchaseRequestItem;
using Infrastructure.Modules.PurchaseRequests.GetPurchaseRequestDetails;
using Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;
using Infrastructure.Modules.PurchaseRequests.RemovePurchaseRequestItem;
using Infrastructure.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;
using Moq;
using DomainPurchaseRequest = global::Domain.Entities.PurchaseRequest;
using DomainPurchaseRequestStatus = global::Domain.Enums.PurchaseRequestStatus;

namespace UnitTests.Modules.PurchaseRequests;

public sealed class PurchaseRequestHandlerTests
{
    private readonly Mock<IPurchaseRequestMembershipReader> _membershipReader = new();
    private readonly Mock<IPurchaseRequestDraftStore> _draftStore = new();
    private readonly Mock<ISelectableProductReader> _productReader = new();
    private readonly Mock<IGetPurchaseRequestDetailsStore> _detailsStore = new();
    private readonly Mock<IListMyPurchaseRequestsStore> _listStore = new();

    [Fact]
    public async Task Add_handler_copies_product_snapshot_and_rotates_stamp()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = DomainPurchaseRequest.Create(userId, organizationId, branchId);
        var membership = CreateMembership(userId, organizationId, branchId);
        var product = new SelectableProductView(
            productId,
            organizationId,
            "Monitor",
            "MON-1",
            "Piece",
            "pc",
            10.50m);
        var command = new AddPurchaseRequestItemCommand(
            userId,
            organizationId,
            request.Id,
            productId,
            2.5m,
            "Two screens",
            request.ConcurrencyStamp);

        SetupMembership(command.UserId, command.OrganizationId, membership);
        _draftStore
            .Setup(store => store.GetOwnedDraftAsync(
                membership,
                command.PurchaseRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _productReader
            .Setup(reader => reader.GetSelectableProductAsync(
                organizationId,
                productId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _draftStore
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateAddHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Monitor", item.ProductName);
        Assert.Equal("MON-1", item.ProductCode);
        Assert.Equal("Piece", item.UnitName);
        Assert.Equal("pc", item.UnitSymbol);
        Assert.Equal(10.50m, item.UnitPrice);
        Assert.Equal(2.5m, item.Quantity);
        Assert.Equal(26.25m, result.Value.TotalValue);
        Assert.NotEqual(command.ExpectedConcurrencyStamp, result.Value.ConcurrencyStamp);
        _draftStore.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Add_handler_rejects_a_stale_stamp_before_reading_product()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var request = DomainPurchaseRequest.Create(userId, organizationId, branchId);
        var membership = CreateMembership(userId, organizationId, branchId);
        var command = new AddPurchaseRequestItemCommand(
            userId,
            organizationId,
            request.Id,
            Guid.NewGuid(),
            1m,
            null,
            "stale-stamp");

        SetupMembership(command.UserId, command.OrganizationId, membership);
        _draftStore
            .Setup(store => store.GetOwnedDraftAsync(
                membership,
                command.PurchaseRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        var result = await CreateAddHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(PurchaseRequestOperationStatus.Conflict, result.Status);
        _productReader.Verify(reader => reader.GetSelectableProductAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _draftStore.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_handler_changes_quantity_and_recalculates_total()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var request = DomainPurchaseRequest.Create(userId, organizationId, branchId);
        var item = request.AddItem(
            Guid.NewGuid(),
            "Monitor",
            "MON-1",
            "Piece",
            "pc",
            10.50m,
            2m);
        var membership = CreateMembership(userId, organizationId, branchId);
        var command = new UpdatePurchaseRequestItemQuantityCommand(
            userId,
            organizationId,
            request.Id,
            item.Id,
            3m,
            request.ConcurrencyStamp);

        SetupMembership(command.UserId, command.OrganizationId, membership);
        _draftStore
            .Setup(store => store.GetOwnedDraftAsync(
                membership,
                command.PurchaseRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _draftStore
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateUpdateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3m, Assert.Single(result.Value.Items).Quantity);
        Assert.Equal(31.50m, result.Value.TotalValue);
        _draftStore.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Remove_handler_allows_the_last_item_to_be_removed()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var request = DomainPurchaseRequest.Create(userId, organizationId, branchId);
        var item = request.AddItem(
            Guid.NewGuid(),
            "Monitor",
            "MON-1",
            "Piece",
            "pc",
            10.50m,
            2m);
        var membership = CreateMembership(userId, organizationId, branchId);
        var command = new RemovePurchaseRequestItemCommand(
            userId,
            organizationId,
            request.Id,
            item.Id,
            request.ConcurrencyStamp);

        SetupMembership(command.UserId, command.OrganizationId, membership);
        _draftStore
            .Setup(store => store.GetOwnedDraftAsync(
                membership,
                command.PurchaseRequestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);
        _draftStore
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateRemoveHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0m, result.Value.TotalValue);
        _draftStore.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Details_handler_returns_store_projection_for_active_employee_scope()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        var membership = CreateMembership(userId, organizationId, Guid.NewGuid());
        var projection = new PurchaseRequestDetailsView(
            requestId,
            userId,
            organizationId,
            membership.BranchId!.Value,
            DomainPurchaseRequestStatus.Draft,
            "Office equipment",
            [],
            0m,
            DateTime.UtcNow,
            DateTime.UtcNow,
            "stamp");
        var query = new GetPurchaseRequestDetailsQuery(userId, organizationId, requestId);

        SetupMembership(query.UserId, query.OrganizationId, membership);
        _detailsStore
            .Setup(store => store.QueryAsync(
                membership,
                requestId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(projection);

        var result = await new GetPurchaseRequestDetailsHandler(
            _membershipReader.Object,
            _detailsStore.Object).HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Same(projection, result.Value);
    }

    [Fact]
    public async Task List_handler_returns_store_page_for_active_employee_scope()
    {
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var membership = CreateMembership(userId, organizationId, Guid.NewGuid());
        var page = new PurchaseRequestListView([], 2, 20, 21);
        var query = new ListMyPurchaseRequestsQuery(userId, organizationId, 2, 20);

        SetupMembership(query.UserId, query.OrganizationId, membership);
        _listStore
            .Setup(store => store.QueryAsync(
                membership,
                query.Page,
                query.PageSize,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await new ListMyPurchaseRequestsHandler(
            _membershipReader.Object,
            _listStore.Object).HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Same(page, result.Value);
    }

    private AddPurchaseRequestItemHandler CreateAddHandler()
        => new(_membershipReader.Object, _draftStore.Object, _productReader.Object);

    private UpdatePurchaseRequestItemQuantityHandler CreateUpdateHandler()
        => new(_membershipReader.Object, _draftStore.Object);

    private RemovePurchaseRequestItemHandler CreateRemoveHandler()
        => new(_membershipReader.Object, _draftStore.Object);

    private void SetupMembership(
        Guid userId,
        Guid organizationId,
        PurchaseRequestMembership membership)
    {
        _membershipReader
            .Setup(reader => reader.GetCurrentMembershipAsync(
                userId,
                organizationId,
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