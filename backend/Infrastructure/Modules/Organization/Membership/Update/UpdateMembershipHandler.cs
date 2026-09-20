using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Update;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.Organization.Membership.Update;

public sealed class UpdateMembershipHandler : IUpdateMembershipHandler
{
    private readonly IUpdateMembershipStore _store;

    public UpdateMembershipHandler(IUpdateMembershipStore store)
    {
        _store = store;
    }

    public async Task<MembershipResult<MembershipView>> HandleAsync(
        UpdateMembershipCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrganizationId == Guid.Empty || command.MembershipId == Guid.Empty)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.ValidationError,
                "OrganizationId and MembershipId are required.");
        }

        if (command.BranchId == Guid.Empty)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.ValidationError,
                "BranchId cannot be empty.");
        }

        var organization = await _store.GetOrganizationAsync(
            command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.NotFound,
                "Organization not found.");
        }

        if (organization.IsArchived)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.Conflict,
                "Archived organization cannot accept membership updates.");
        }

        var membership = await _store.GetMembershipForUpdateAsync(
            command.OrganizationId,
            command.MembershipId,
            cancellationToken);

        if (membership is null)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.NotFound,
                "Membership not found.");
        }

        if (!membership.IsActive)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.Conflict,
                "Inactive membership cannot be updated.");
        }

        if (command.BranchId is Guid branchId)
        {
            var branch = await _store.GetActiveBranchAsync(
                command.OrganizationId,
                branchId,
                cancellationToken);

            if (branch is null)
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.NotFound,
                    "Branch not found or is inactive.");
            }
        }

        try
        {
            membership.UpdateAssignment(command.Role, command.BranchId);
        }
        catch (ArgumentException exception)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.ValidationError,
                exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return MembershipResult<MembershipView>.Failure(
                MembershipOperationStatus.Conflict,
                exception.Message);
        }

        await _store.SaveChangesAsync(cancellationToken);

        return MembershipResult<MembershipView>.Success(
            new MembershipView(
                membership.Id,
                membership.OrganizationId,
                membership.UserId,
                membership.BranchId,
                membership.Role,
                membership.IsActive),
            "Membership updated");
    }
}
