namespace Application.Modules.Organization.Membership.GetList;
public interface IListMembershipHandler
{
    Task<MembershipResult<IReadOnlyList<MembershipListItem>>> HandleAsync(
        ListMembershipsQuery query,
        CancellationToken cancellationToken = default);
}
