using Application.Modules.Organization.Membership;

namespace Application.Modules.Organization.Membership.Archive;

public interface IArchiveMembershipHandler
{
    Task<MembershipResult<bool>> HandleAsync(
        ArchiveMembershipCommand command,
        CancellationToken cancellationToken = default);
}
