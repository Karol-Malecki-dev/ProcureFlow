using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.GetList;

public sealed record MembershipListItem(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    Guid? BranchId,
    string? BranchName,
    BusinessRole Role,
    bool IsActive);

