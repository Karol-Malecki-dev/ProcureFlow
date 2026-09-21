using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public interface ICreateUnitOfMeasureStore
{
    Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageCatalogAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveSymbolExistsAsync(
        Guid organizationId,
        string symbol,
        CancellationToken cancellationToken = default);

    void Add(DomainUnitOfMeasure unitOfMeasure);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}