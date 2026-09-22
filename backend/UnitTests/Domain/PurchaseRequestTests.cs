using Domain.Entities;
using Domain.Enums;

namespace UnitTests.Domain;

public sealed class PurchaseRequestTests
{
    [Fact]
    public void Create_starts_with_an_empty_draft_and_normalizes_note()
    {
        var authorId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var request = PurchaseRequest.Create(
            authorId,
            organizationId,
            branchId,
            "  Need office supplies  ");

        Assert.Equal(authorId, request.AuthorUserId);
        Assert.Equal(organizationId, request.OrganizationId);
        Assert.Equal(branchId, request.BranchId);
        Assert.Equal(PurchaseRequestStatus.Draft, request.Status);
        Assert.Equal("Need office supplies", request.Note);
        Assert.Empty(request.Items);
        Assert.Equal(0m, request.TotalValue);
        Assert.False(string.IsNullOrWhiteSpace(request.ConcurrencyStamp));
    }

    [Fact]
    public void Add_item_captures_snapshot_and_calculates_total()
    {
        var request = CreateRequest();
        var initialStamp = request.ConcurrencyStamp;

        var item = request.AddItem(
            Guid.NewGuid(),
            "  Printer paper  ",
            " PAPER-80",
            "Pack",
            "PKG",
            12.50m,
            2.5m,
            "  White paper  ");

        Assert.Equal("Printer paper", item.ProductNameSnapshot);
        Assert.Equal("PAPER-80", item.ProductCodeSnapshot);
        Assert.Equal("Pack", item.UnitNameSnapshot);
        Assert.Equal("pkg", item.UnitSymbolSnapshot);
        Assert.Equal("White paper", item.Comment);
        Assert.Equal(31.25m, item.LineTotal);
        Assert.Equal(31.25m, request.TotalValue);
        Assert.NotEqual(initialStamp, request.ConcurrencyStamp);
    }

    [Fact]
    public void Update_quantity_preserves_snapshot_and_recalculates_total()
    {
        var request = CreateRequest();
        var item = request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            2m);
        var initialStamp = request.ConcurrencyStamp;

        request.UpdateItemQuantity(item.Id, 3m);

        Assert.Equal(3m, item.Quantity);
        Assert.Equal(12.50m, item.UnitPriceSnapshot);
        Assert.Equal("Printer paper", item.ProductNameSnapshot);
        Assert.Equal(37.50m, request.TotalValue);
        Assert.NotEqual(initialStamp, request.ConcurrencyStamp);
    }

    [Fact]
    public void Removing_the_last_item_leaves_an_empty_valid_draft()
    {
        var request = CreateRequest();
        var item = request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            2m);

        request.RemoveItem(item.Id);

        Assert.Empty(request.Items);
        Assert.Equal(0m, request.TotalValue);
        Assert.Equal(PurchaseRequestStatus.Draft, request.Status);
    }

    [Fact]
    public void Submit_rejects_an_empty_draft()
    {
        var request = CreateRequest();

        var exception = Assert.Throws<InvalidOperationException>(() => request.Submit());

        Assert.Contains("at least one item", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(PurchaseRequestStatus.Draft, request.Status);
    }

    [Fact]
    public void Submit_changes_status_and_rotates_concurrency_stamp()
    {
        var request = CreateRequest();
        request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            2m);
        var initialStamp = request.ConcurrencyStamp;

        request.Submit();

        Assert.Equal(PurchaseRequestStatus.Submitted, request.Status);
        Assert.NotEqual(initialStamp, request.ConcurrencyStamp);
    }

    [Fact]
    public void Cancel_is_allowed_for_draft_and_submitted_requests()
    {
        var draft = CreateRequest();
        draft.Cancel();

        var submitted = CreateRequest();
        submitted.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            1m);
        submitted.Submit();
        submitted.Cancel();

        Assert.Equal(PurchaseRequestStatus.Cancelled, draft.Status);
        Assert.Equal(PurchaseRequestStatus.Cancelled, submitted.Status);
    }

    [Fact]
    public void Cancelled_request_cannot_be_submitted_or_cancelled_again()
    {
        var request = CreateRequest();
        request.Cancel();

        Assert.Throws<InvalidOperationException>(() => request.Submit());
        Assert.Throws<InvalidOperationException>(() => request.Cancel());
    }

    [Fact]
    public void Submitted_request_cannot_be_edited()
    {
        var request = CreateRequest();
        var item = request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            1m);
        request.Submit();

        Assert.Throws<InvalidOperationException>(() => request.UpdateItemQuantity(item.Id, 2m));
        Assert.Throws<InvalidOperationException>(() => request.RemoveItem(item.Id));
        Assert.Throws<InvalidOperationException>(() => request.AddItem(
            Guid.NewGuid(),
            "Ink",
            null,
            "Piece",
            "pc",
            5m,
            1m));
    }

    [Fact]
    public void Approved_request_can_be_marked_as_ordered_with_fulfillment_metadata()
    {
        var request = CreateRequestInStatus(PurchaseRequestStatus.Approved);
        var initialStamp = request.ConcurrencyStamp;

        request.MarkOrdered("  PO-2026-001  ", "  Ordered by Procurement  ");

        Assert.Equal(PurchaseRequestStatus.Ordered, request.Status);
        Assert.Equal("PO-2026-001", request.FulfillmentOrderNumber);
        Assert.Equal("Ordered by Procurement", request.FulfillmentNote);
        Assert.NotEqual(initialStamp, request.ConcurrencyStamp);
    }

    [Fact]
    public void Only_ordered_request_can_be_marked_as_delivered()
    {
        var request = CreateRequestInStatus(PurchaseRequestStatus.Approved);

        Assert.Throws<InvalidOperationException>(() => request.MarkDelivered());

        request.MarkOrdered();
        request.MarkDelivered("  Received in branch warehouse  ");

        Assert.Equal(PurchaseRequestStatus.Delivered, request.Status);
        Assert.Equal("Received in branch warehouse", request.FulfillmentNote);
    }

    [Fact]
    public void Delivered_request_is_final_and_cannot_be_marked_as_ordered_again()
    {
        var request = CreateRequestInStatus(PurchaseRequestStatus.Approved);
        request.MarkOrdered("PO-2026-001");
        request.MarkDelivered();

        Assert.Throws<InvalidOperationException>(() => request.MarkOrdered("PO-2026-002"));
        Assert.Throws<InvalidOperationException>(() => request.MarkDelivered());
        Assert.Equal(PurchaseRequestStatus.Delivered, request.Status);
        Assert.Equal("PO-2026-001", request.FulfillmentOrderNumber);
    }

    [Theory]
    [InlineData(nameof(PurchaseRequest.FulfillmentOrderNumber), 101)]
    [InlineData(nameof(PurchaseRequest.FulfillmentNote), 1_001)]
    public void Fulfillment_metadata_rejects_values_above_the_contract_limit(
        string field,
        int length)
    {
        var request = CreateRequestInStatus(PurchaseRequestStatus.Approved);

        var exception = Assert.Throws<ArgumentException>(() => request.MarkOrdered(
            field == nameof(PurchaseRequest.FulfillmentOrderNumber)
                ? new string('x', length)
                : null,
            field == nameof(PurchaseRequest.FulfillmentNote)
                ? new string('x', length)
                : null));

        Assert.Contains("cannot exceed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_item_rejects_a_duplicate_product()
    {
        var request = CreateRequest();
        var productId = Guid.NewGuid();
        request.AddItem(productId, "Printer paper", null, "Pack", "pkg", 12.50m, 1m);

        var exception = Assert.Throws<InvalidOperationException>(() => request.AddItem(
            productId,
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            2m));

        Assert.Contains("already present", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(request.Items);
        Assert.Equal(12.50m, request.TotalValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_000.001)]
    [InlineData(1.2345)]
    public void Add_item_rejects_invalid_quantity(decimal quantity)
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentOutOfRangeException>(() => request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            quantity));
    }

    [Fact]
    public void Add_item_rejects_negative_or_over_precision_price()
    {
        var request = CreateRequest();

        Assert.Throws<ArgumentOutOfRangeException>(() => request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            -0.01m,
            1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.345m,
            1m));
    }

    [Fact]
    public void Create_rejects_empty_scope_identifiers()
    {
        Assert.Throws<ArgumentException>(() => PurchaseRequest.Create(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => PurchaseRequest.Create(
            Guid.NewGuid(),
            Guid.Empty,
            Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => PurchaseRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.Empty));
    }

    private static PurchaseRequest CreateRequest()
        => PurchaseRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

    private static PurchaseRequest CreateRequestInStatus(PurchaseRequestStatus status)
    {
        var request = CreateRequest();
        request.AddItem(
            Guid.NewGuid(),
            "Printer paper",
            null,
            "Pack",
            "pkg",
            12.50m,
            1m);
        request.Submit();

        if (status == PurchaseRequestStatus.Approved)
        {
            request.Approve();
        }

        return request;
    }
}
