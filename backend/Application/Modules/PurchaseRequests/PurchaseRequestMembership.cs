using Domain.Models.Organizations.Enums;

namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Trusted organization scope resolved from the current user's active membership.
/// </summary>
public sealed record PurchaseRequestMembership(
    Guid MembershipId,
    Guid UserId,
    Guid OrganizationId,
    Guid? BranchId,
    BusinessRole Role,
    bool IsUserActive,
    bool IsOrganizationActive,
    bool IsBranchActive)
{
    public bool IsActiveEmployeeScope
        => Role == BusinessRole.Employee
            && BranchId.HasValue
            && IsUserActive
            && IsOrganizationActive
            && IsBranchActive;
}
