using DomainBranch = Domain.Models.Organizations.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Application.Modules.Organization.Branch.CreateBranch;

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
