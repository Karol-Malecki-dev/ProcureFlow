using DomainOrganization = Domain.Models.Organizations.Organization.Organization;

namespace Application.Modules.Organization.ArchiveBranch;

public interface IArchiveBranchStore
{
    Task<DomainOrganization?> GetOrganizationWithBranchesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
