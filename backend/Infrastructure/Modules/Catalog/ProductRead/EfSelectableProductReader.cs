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
        => _dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == productId
                && product.OrganizationId == organizationId
                && product.IsActive
                && product.IsAvailable)
            .Join(
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
                    product.UnitPrice))
            .SingleOrDefaultAsync(cancellationToken);
}
