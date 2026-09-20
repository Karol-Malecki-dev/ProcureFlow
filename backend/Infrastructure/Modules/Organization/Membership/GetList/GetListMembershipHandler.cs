using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.GetList;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.Organization.Membership.GetList;

public sealed class GetListMembershipHandler : IListMembershipHandler
{
    private readonly IListMembershipStore _store;
    public GetListMembershipHandler(IListMembershipStore store)
    {
        _store = store;
    }
    public async Task<MembershipResult<IReadOnlyList<MembershipListItem>>> HandleAsync(
        ListMembershipsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.OrganizationId == Guid.Empty)
        {
            return MembershipResult<IReadOnlyList<MembershipListItem>>.Failure(
                MembershipOperationStatus.ValidationError,
                "OrganizationId is required.");
        }

        if (query.BranchId is Guid branchId && branchId == Guid.Empty)
        {
            return MembershipResult<IReadOnlyList<MembershipListItem>>.Failure(
                MembershipOperationStatus.ValidationError,
                "BranchId cannot be empty.");
        }

        if (query.Role.HasValue && !Enum.IsDefined(query.Role.Value))
        {
            return MembershipResult<IReadOnlyList<MembershipListItem>>.Failure(
                MembershipOperationStatus.ValidationError,
                "Role is invalid.");
        }

        if (!await _store.OrganizationExistsAsync(
                query.OrganizationId,
                cancellationToken))
        {
            return MembershipResult<IReadOnlyList<MembershipListItem>>.Failure(
                MembershipOperationStatus.NotFound,
                "Organization not found.");
        }

        var memberships = await _store.QueryAsync(query, cancellationToken);

        return MembershipResult<IReadOnlyList<MembershipListItem>>.Success(
            memberships,
            "Memberships retrieved.");
    }
}
