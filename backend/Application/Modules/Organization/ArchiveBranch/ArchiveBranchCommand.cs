namespace Application.Modules.Organization.ArchiveBranch;

public sealed record ArchiveBranchCommand(
    Guid OrganizationId,
    Guid BranchId);
