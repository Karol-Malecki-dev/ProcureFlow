using Domain.Models.Organizations.Enums;

namespace API.Modules.Organization.Membership.List;

public sealed record MembershipListResponse(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    Guid? BranchId,
    string? BranchName,
    BusinessRole Role,
    bool IsActive);