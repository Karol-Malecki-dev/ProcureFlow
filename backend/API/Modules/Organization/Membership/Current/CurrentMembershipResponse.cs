using Domain.Models.Organizations.Enums;

namespace API.Modules.Organization.Membership.Current;

public sealed record CurrentMembershipResponse(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    Guid? BranchId,
    BusinessRole Role,
    bool IsActive);