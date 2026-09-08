using Domain.ValueObjects;

namespace Application.Modules.Organization.ListBranches;

public sealed record BranchListItem(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);
