
using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership
{
    public sealed record MembershipView(
        Guid Id,
        Guid OrganizationId,
        Guid UserId,
        Guid? BranchId,
        BusinessRole Role,
        bool IsActive);
    
}
