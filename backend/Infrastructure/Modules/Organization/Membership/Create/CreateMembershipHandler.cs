
using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.Create;
using Domain.Models.Organizations.Enums;
using DomainMembership = Domain.Models.Organizations.Membership;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.Create
{
    public sealed class CreateMembershipHandler : ICreateMembershipHandler
    {
        private readonly ICreateMembershipStore _store;
        public CreateMembershipHandler(ICreateMembershipStore store)
        {
            _store = store;
        }
        public async Task<MembershipResult<MembershipView>> HandleAsync(
            CreateMembershipCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command.OrganizationId == Guid.Empty || command.UserId == Guid.Empty)
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.ValidationError,
                    "OrganizationId and UserId are required.");
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
                    "Organization does not exist.");
            }

            if (organization.IsArchived)
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.Conflict,
                    "Archived organization cannot accept memberships.");
            }

            var user = await _store.GetActiveUserAsync(
                command.UserId,
                cancellationToken);

            if (user is null)
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.NotFound,
                    "User does not exist or is inactive.");
            }

            if (await _store.ActiveMembershipExistsAsync(
                    command.UserId,
                    cancellationToken))
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.Conflict,
                    "User already has an active membership.");
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
                        "Branch does not exist, is inactive, or belongs to another organization.");
                }
            }

            DomainMembership membership;
            try
            {
                membership = new DomainMembership(
                    command.OrganizationId,
                    command.UserId,
                    command.BranchId,
                    command.Role);
            }
            catch (ArgumentException exception)
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.ValidationError,
                    exception.Message);
            }

            _store.Add(membership);

            try
            {
                await _store.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                    exception,
                    "UX_Memberships_ActiveUser"))
            {
                return MembershipResult<MembershipView>.Failure(
                    MembershipOperationStatus.Conflict,
                    "User already has an active membership.");
            }

            return MembershipResult<MembershipView>.Success(
                new MembershipView(
                    membership.Id,
                    membership.OrganizationId,
                    membership.UserId,
                    membership.BranchId,
                    membership.Role,
                    membership.IsActive),
                "Membership created");
        }

    }
}
