using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Get;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.Organization.Membership.Get;

public class GetMembershipDetailsHandler : IGetMembershipDetailsHandler
{
    private readonly IGetMembershipDetailsStore _store;
    public GetMembershipDetailsHandler(IGetMembershipDetailsStore store)
    {
        _store = store;
    }
    public async Task<MembershipResult<MembershipDetails>> HandleAsync(GetMembershipQueary query, CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty)
        {
            return MembershipResult<MembershipDetails>.Failure(
                MembershipOperationStatus.ValidationError,
                "UserId is required.");
        }

        var membership = await _store.GetMembershipDetailsAsync(query.UserId, cancellationToken);

        if (membership is null)
        {
            return MembershipResult<MembershipDetails>.Failure(
                MembershipOperationStatus.NotFound,
                "Membership not found."
                );
        }

        return MembershipResult<MembershipDetails>.Success(membership);
    }
}
