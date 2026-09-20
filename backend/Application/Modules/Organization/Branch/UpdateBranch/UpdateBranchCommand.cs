using Domain.ValueObjects;

namespace Application.Modules.Organization.Branch.UpdateBranch;

public sealed record UpdateBranchCommand(
    Guid OrganizationId,
    Guid BranchId,
    string Name,
    string Code,
    Address Address);
