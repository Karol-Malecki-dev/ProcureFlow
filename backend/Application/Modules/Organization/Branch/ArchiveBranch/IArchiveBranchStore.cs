using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Application.Modules.Organization.Branch.ArchiveBranch;

public interface IArchiveBranchStore
{
    Task<DomainOrganization?> GetOrganizationWithBranchesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
