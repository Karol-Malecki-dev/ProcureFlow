using DomainMembership = Domain.Models.Organizations.Membership;

namespace Application.Modules.Organization.Membership.Archive;

public interface IArchiveMembershipStore
{
    Task<DomainMembership?> GetMembershipForUpdateAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
