namespace Application.Modules.Organization.GetBranchDetails;

public sealed record GetBranchDetailsQuery(
    Guid OrganizationId,
    Guid BranchId,
    bool IncludeArchived = false);
