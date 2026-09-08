using Domain.ValueObjects;

namespace Application.Modules.Organization.CreateBranch;

public sealed record BranchView(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    Address Address);