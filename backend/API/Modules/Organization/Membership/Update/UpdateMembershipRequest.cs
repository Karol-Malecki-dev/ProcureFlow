using Domain.Models.Organizations.Enums;

namespace API.Modules.Organization.Membership.Update;

public sealed record UpdateMembershipRequest(
    Guid? BranchId,
    BusinessRole Role);