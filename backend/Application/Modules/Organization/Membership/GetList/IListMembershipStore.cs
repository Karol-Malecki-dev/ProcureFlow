namespace Application.Modules.Organization.Membership.GetList;
public interface IListMembershipStore
{
    Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipListItem>> QueryAsync(
        ListMembershipsQuery query,
        CancellationToken cancellationToken = default);
}
