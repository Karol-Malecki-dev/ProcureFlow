using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Archive;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.Organization.Membership.Archive;

public sealed class ArchiveMembershipHandler : IArchiveMembershipHandler
{
    private readonly IArchiveMembershipStore _store;

    public ArchiveMembershipHandler(
        IArchiveMembershipStore store)
    {
        _store = store;
    }

    public async Task<MembershipResult<bool>> HandleAsync(
        ArchiveMembershipCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrganizationId == Guid.Empty || command.MembershipId == Guid.Empty)
        {
            return MembershipResult<bool>.Failure(
                MembershipOperationStatus.ValidationError,
                "OrganizationId and MembershipId are required.");
        }

        var membership = await _store.GetMembershipForUpdateAsync(
            command.OrganizationId,
            command.MembershipId,
            cancellationToken);

        if (membership is null)
        {
            return MembershipResult<bool>.Failure(
                MembershipOperationStatus.NotFound,
                "Membership not found.");
        }

        if (!membership.IsActive)
        {
            return MembershipResult<bool>.Success(
                true,
                "Membership already inactive.");
        }

        try
        {
            membership.Deactivate();
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return MembershipResult<bool>.Failure(
                MembershipOperationStatus.Conflict,
                exception.Message);
        }

        return MembershipResult<bool>.Success(
            true,
            "Membership deactivated.");

    }
}
