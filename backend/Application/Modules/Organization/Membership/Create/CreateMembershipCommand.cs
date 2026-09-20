
using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.Create
{
    public sealed record CreateMembershipCommand(
        Guid OrganizationId,
        Guid UserId,
        Guid? BranchId,
        BusinessRole Role);
}
