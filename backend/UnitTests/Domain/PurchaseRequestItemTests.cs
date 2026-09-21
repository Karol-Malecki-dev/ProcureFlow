using Domain.Entities;

namespace UnitTests.Domain;

public sealed class PurchaseRequestItemTests
{
    [Fact]
    public void Item_line_total_rounds_away_from_zero_to_two_places()
    {
        var request = PurchaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var item = request.AddItem(
            Guid.NewGuid(),
            "Rounding sample",
            null,
            "Piece",
            "pcs",
            1.00m,
            1.005m);

        Assert.Equal(1.01m, item.LineTotal);
        Assert.Equal(1.01m, request.TotalValue);
    }

    [Theory]
    [InlineData(1_000_000.001)]
    [InlineData(0.0001)]
    public void Quantity_precision_is_limited_to_three_decimal_places(decimal quantity)
    {
        var request = PurchaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<ArgumentOutOfRangeException>(() => request.AddItem(
            Guid.NewGuid(),
            "Quantity sample",
            null,
            "Piece",
            "pcs",
            1m,
            quantity));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Blank_optional_snapshot_values_are_stored_as_null(string? value)
    {
        var request = PurchaseRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var item = request.AddItem(
            Guid.NewGuid(),
            "Optional fields sample",
            value,
            "Piece",
            "pcs",
            1m,
            1m,
            value);

        Assert.Null(item.ProductCodeSnapshot);
        Assert.Null(item.Comment);
    }
}
