using Domain.Models.Catalog;

namespace UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_normalizes_product_fields_and_starts_selectable()
    {
        var product = new Product(
            Guid.NewGuid(),
            "  Printer paper  ",
            " PAPER-80 ",
            Guid.NewGuid(),
            12.50m,
            Guid.NewGuid());

        Assert.Equal("Printer paper", product.Name);
        Assert.Equal("PAPER-80", product.Code);
        Assert.True(product.IsActive);
        Assert.True(product.IsAvailable);
    }

    [Fact]
    public void Archive_removes_product_from_new_request_selection()
    {
        var product = new Product(
            Guid.NewGuid(),
            "Printer paper",
            null,
            Guid.NewGuid(),
            12.50m,
            Guid.NewGuid());

        product.Archive(Guid.NewGuid());

        Assert.False(product.IsActive);
        Assert.False(product.IsAvailable);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(12.345)]
    public void Create_rejects_invalid_unit_price(decimal unitPrice)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Product(
            Guid.NewGuid(),
            "Printer paper",
            null,
            Guid.NewGuid(),
            unitPrice,
            Guid.NewGuid()));
    }
}
