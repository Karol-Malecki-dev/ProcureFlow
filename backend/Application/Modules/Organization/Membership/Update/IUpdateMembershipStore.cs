using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Application.Modules.Organization.Membership.Update;

public interface IUpdateMembershipStore
{
    Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<DomainMembership?> GetMembershipForUpdateAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<DomainBranch?> GetActiveBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}