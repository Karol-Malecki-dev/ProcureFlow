using Application.Modules.Catalog.ProductRead;
using Domain.Models.Catalog;
using Infrastructure.Data;
using Infrastructure.Modules.Catalog.ProductRead;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Modules.Catalog.ProductRead;

public sealed class EfSelectableProductReaderTests
{
    [Fact]
    public async Task Reader_returns_only_active_available_products_with_active_units()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"product-read-{Guid.NewGuid():N}")
            .Options;
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var activeUnit = new UnitOfMeasure(organizationId, "Piece", "pcs", userId);
        var foreignOrganizationId = Guid.NewGuid();
        var foreignUnit = new UnitOfMeasure(foreignOrganizationId, "Box", "box", userId);
        var selectable = new Product(organizationId, "Printer paper", "PAPER-80", activeUnit.Id, 12.50m, userId);
        var archived = new Product(organizationId, "Archived paper", "PAPER-ARCHIVED", activeUnit.Id, 10m, userId);
        archived.Archive(userId);
        var unavailable = new Product(organizationId, "Unavailable paper", "PAPER-OFF", activeUnit.Id, 10m, userId);
        unavailable.SetAvailability(false, userId);
        var foreignUnitProduct = new Product(organizationId, "Boxed paper", "PAPER-BOX", foreignUnit.Id, 10m, userId);

        await using (var context = new ApplicationDbContext(options))
        {
            context.UnitsOfMeasure.AddRange(activeUnit, foreignUnit);
            context.Products.AddRange(selectable, archived, unavailable, foreignUnitProduct);
            await context.SaveChangesAsync();
        }

        await using var readerContext = new ApplicationDbContext(options);
        var reader = new EfSelectableProductReader(readerContext);

        var result = await reader.GetSelectableProductAsync(organizationId, selectable.Id);

        Assert.NotNull(result);
        Assert.Equal(selectable.Id, result.ProductId);
        Assert.Equal("Printer paper", result.Name);
        Assert.Equal("PAPER-80", result.Code);
        Assert.Equal("Piece", result.UnitName);
        Assert.Equal("pcs", result.UnitSymbol);
        Assert.Equal(12.50m, result.UnitPrice);
        Assert.Null(await reader.GetSelectableProductAsync(organizationId, archived.Id));
        Assert.Null(await reader.GetSelectableProductAsync(organizationId, unavailable.Id));
        Assert.Null(await reader.GetSelectableProductAsync(organizationId, foreignUnitProduct.Id));

        var products = await reader.GetSelectableProductsAsync(organizationId);

        var listedProduct = Assert.Single(products);
        Assert.Equal(selectable.Id, listedProduct.ProductId);
    }
}
