namespace Application.Modules.Organization.Branch.GetBranchDetails;

public sealed record GetBranchDetailsQuery(
    Guid OrganizationId,
    Guid BranchId,
    bool IncludeArchived = false);
