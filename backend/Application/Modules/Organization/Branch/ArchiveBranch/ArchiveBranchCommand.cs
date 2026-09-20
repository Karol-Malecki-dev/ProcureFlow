namespace Application.Modules.Organization.Branch.ArchiveBranch;

public sealed record ArchiveBranchCommand(
    Guid OrganizationId,
    Guid BranchId);
