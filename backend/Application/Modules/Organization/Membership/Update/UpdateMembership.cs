using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.Update;

public sealed record UpdateMembership(
    Guid OrganizationId,
    Guid UserId,
    Guid BranchId,
    BusinessRole Role,
    bool IsActive
);
