using Application.Modules.Organization.Membership;

namespace Application.Modules.Organization.Membership.Update;

public interface IUpdateMembershipHandler
{
    Task<MembershipResult<MembershipView>> HandleAsync(
        UpdateMembershipCommand command,
        CancellationToken cancellationToken = default);
}