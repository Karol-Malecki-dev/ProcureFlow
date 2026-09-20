using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.Update;

public sealed record UpdateMembershipCommand(
    Guid MembershipId,
    Guid OrganizationId,
    Guid? BranchId,
    BusinessRole Role
);
    
