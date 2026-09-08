using DomainBranch = Domain.Models.Organizations.Organization.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization.Organization;

namespace Application.Modules.Organization.CreateBranch;

public interface ICreateBranchStore
{
    Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        CancellationToken cancellationToken = default);

    void Add(DomainBranch branch);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
