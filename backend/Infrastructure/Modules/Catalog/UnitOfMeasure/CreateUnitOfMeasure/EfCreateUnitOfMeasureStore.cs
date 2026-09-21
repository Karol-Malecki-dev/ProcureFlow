using Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using Domain.Enums;
using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Infrastructure.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public sealed class EfCreateUnitOfMeasureStore : ICreateUnitOfMeasureStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfCreateUnitOfMeasureStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => await _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                organization => organization.Id == organizationId,
                cancellationToken);

    public async Task<bool> CanManageCatalogAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
        => await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == userId
                    && user.IsActive
                    && (user.Role == UserRole.Admin
                        || _dbContext.Memberships.Any(membership =>
                            membership.OrganizationId == organizationId
                            && membership.UserId == userId
                            && membership.IsActive
                            && membership.Role == BusinessRole.Procurement)),
                cancellationToken);

    public async Task<bool> ActiveSymbolExistsAsync(
        Guid organizationId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToLowerInvariant();

        return await _dbContext.UnitsOfMeasure
            .AsNoTracking()
            .AnyAsync(
                unit => unit.OrganizationId == organizationId
                    && unit.IsActive
                    && unit.Symbol == normalizedSymbol,
                cancellationToken);
    }

    public void Add(DomainUnitOfMeasure unitOfMeasure)
    {
        _dbContext.UnitsOfMeasure.Add(unitOfMeasure);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}