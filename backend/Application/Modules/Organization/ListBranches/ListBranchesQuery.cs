namespace Application.Modules.Organization.ListBranches;

public sealed record ListBranchesQuery(
    Guid OrganizationId,
    bool IncludeArchived = false);
