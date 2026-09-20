namespace Application.Modules.Organization.Membership.Get;

public interface IGetMembershipDetailsStore
{
    Task<MembershipDetails?> GetMembershipDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
