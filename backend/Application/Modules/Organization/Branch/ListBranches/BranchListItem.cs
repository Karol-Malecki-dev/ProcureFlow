using Domain.ValueObjects;

namespace Application.Modules.Organization.Branch.ListBranches;

public sealed record BranchListItem(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);
