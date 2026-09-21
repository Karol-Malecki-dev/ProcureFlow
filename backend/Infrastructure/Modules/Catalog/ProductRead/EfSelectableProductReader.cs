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
        => BuildSelectableProductsQuery(organizationId)
            .Where(product => product.ProductId == productId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<SelectableProductView>> GetSelectableProductsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => await BuildSelectableProductsQuery(organizationId)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.ProductId)
            .ToListAsync(cancellationToken);

    private IQueryable<SelectableProductView> BuildSelectableProductsQuery(Guid organizationId)
        => _dbContext.Products
            .AsNoTracking()
            .Where(product => product.OrganizationId == organizationId
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
                    ;
}
