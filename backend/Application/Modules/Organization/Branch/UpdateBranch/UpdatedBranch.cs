using Domain.ValueObjects;

namespace Application.Modules.Organization.Branch.UpdateBranch;

public sealed record UpdatedBranch(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);
