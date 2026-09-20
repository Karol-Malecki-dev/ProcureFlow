using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership.GetList;

public sealed record ListMembershipsQuery(
    Guid OrganizationId,
    Guid? BranchId = null,
    BusinessRole? Role = null,
    bool IncludeInactive = false);
