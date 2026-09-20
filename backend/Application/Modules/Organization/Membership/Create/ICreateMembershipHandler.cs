using Application.Modules.Organization.Membership;

namespace Application.Modules.Organization.Membership.Create
{
    public interface ICreateMembershipHandler
    {
        Task<MembershipResult<MembershipView>> HandleAsync(
            CreateMembershipCommand command,
            CancellationToken cancellationToken = default);
    }
}
