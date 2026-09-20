
using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.Get;

public sealed record MembershipDetails(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    Guid? BranchId,
    BusinessRole Role,
    bool IsActive);


