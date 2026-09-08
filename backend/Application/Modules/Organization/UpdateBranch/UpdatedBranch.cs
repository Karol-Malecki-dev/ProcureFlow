using Domain.ValueObjects;

namespace Application.Modules.Organization.UpdateBranch;

public sealed record UpdatedBranch(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);
