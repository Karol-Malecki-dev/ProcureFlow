using Domain.Models.Organizations.Enums;

namespace API.Modules.Organization.Membership;

public sealed record MembershipResponse(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    Guid? BranchId,
    BusinessRole Role,
    bool IsActive);