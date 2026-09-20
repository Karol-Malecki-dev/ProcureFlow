using Domain.ValueObjects;

namespace Application.Modules.Organization.Branch.GetBranchDetails;

public sealed record BranchDetails(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);
