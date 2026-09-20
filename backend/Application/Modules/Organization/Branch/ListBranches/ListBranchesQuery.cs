namespace Application.Modules.Organization.Branch.ListBranches;

public sealed record ListBranchesQuery(
    Guid OrganizationId,
    bool IncludeArchived = false);
