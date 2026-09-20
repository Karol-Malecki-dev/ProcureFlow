using Domain.Models.Organizations.Enums;

namespace API.Modules.Organization.Membership.Create;

public sealed record CreateMembershipRequest(
    Guid UserId,
    Guid? BranchId,
    BusinessRole Role);