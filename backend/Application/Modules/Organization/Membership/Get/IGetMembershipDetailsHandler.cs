namespace Application.Modules.Organization.Membership.Get;

public interface IGetMembershipDetailsHandler
{
    Task<MembershipResult<MembershipDetails>> HandleAsync(
        GetMembershipQueary query,
        CancellationToken cancellationToken = default);
}
