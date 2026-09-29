using Application.Modules.Catalog.ProductRead;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Catalog.ProductRead;

/// <summary>
/// Reads the server-owned product data required to create a request-item snapshot.
/// </summary>
public sealed class EfSelectableProductReader : ISelectableProductReader
{
    private readonly ApplicationDbContext _dbContext;

    public EfSelectableProductReader(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SelectableProductView?> GetSelectableProductAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
        => BuildSelectableProductsQuery(organizationId, productId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SelectableProductView>> GetSelectableProductsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var products = await BuildSelectableProductsQuery(organizationId)
            .ToListAsync(cancellationToken);

        return products
            .OrderBy(product => product.Name)
            .ThenBy(product => product.ProductId)
            .ToList();
    }

    private IQueryable<SelectableProductView> BuildSelectableProductsQuery(
        Guid organizationId,
        Guid? productId = null)
    {
        var products = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.OrganizationId == organizationId
                && product.IsActive
                && product.IsAvailable);

        if (productId.HasValue)
        {
            products = products.Where(product => product.Id == productId.Value);
        }

        return products.Join(
                _dbContext.UnitsOfMeasure
                    .AsNoTracking()
                    .Where(unit => unit.OrganizationId == organizationId && unit.IsActive),
                product => product.UnitOfMeasureId,
                unit => unit.Id,
                (product, unit) => new SelectableProductView(
                    product.Id,
                    product.OrganizationId,
                    product.Name,
                    product.Code,
                    unit.Name,
                    unit.Symbol,
                    product.UnitPrice));
    }
}
