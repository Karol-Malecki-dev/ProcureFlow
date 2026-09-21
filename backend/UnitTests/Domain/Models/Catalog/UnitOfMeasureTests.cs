using Domain.Models.Catalog;

namespace UnitTests.Domain.Models.Catalog;

public sealed class UnitOfMeasureTests
{
    [Fact]
    public void Constructor_creates_active_unit_with_normalized_values()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 9, 21, 12, 0, 0, DateTimeKind.Utc);

        var unit = new UnitOfMeasure(
            organizationId,
            " Kilogram ",
            " KG ",
            userId,
            createdAt);

        Assert.NotEqual(Guid.Empty, unit.Id);
        Assert.Equal(organizationId, unit.OrganizationId);
        Assert.Equal("Kilogram", unit.Name);
        Assert.Equal("kg", unit.Symbol);
        Assert.True(unit.IsActive);
        Assert.Equal(createdAt, unit.CreatedAt);
        Assert.Equal(createdAt, unit.UpdatedAt);
        Assert.Equal(userId, unit.CreatedByUserId);
        Assert.Equal(userId, unit.UpdatedByUserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_rejects_blank_name(string name)
    {
        var exception = Assert.Throws<ArgumentException>(() => new UnitOfMeasure(
            Guid.NewGuid(),
            name,
            "kg",
            Guid.NewGuid()));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_rejects_blank_symbol(string symbol)
    {
        var exception = Assert.Throws<ArgumentException>(() => new UnitOfMeasure(
            Guid.NewGuid(),
            "Kilogram",
            symbol,
            Guid.NewGuid()));

        Assert.Equal("symbol", exception.ParamName);
    }

    [Fact]
    public void Constructor_rejects_empty_organization_id()
    {
        var exception = Assert.Throws<ArgumentException>(() => new UnitOfMeasure(
            Guid.Empty,
            "Kilogram",
            "kg",
            Guid.NewGuid()));

        Assert.Equal("organizationId", exception.ParamName);
    }

    [Fact]
    public void Constructor_rejects_empty_creator_id()
    {
        var exception = Assert.Throws<ArgumentException>(() => new UnitOfMeasure(
            Guid.NewGuid(),
            "Kilogram",
            "kg",
            Guid.Empty));

        Assert.Equal("createdByUserId", exception.ParamName);
    }
}