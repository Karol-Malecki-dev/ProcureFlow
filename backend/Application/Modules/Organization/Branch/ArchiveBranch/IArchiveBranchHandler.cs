namespace Application.Modules.Organization.Branch.ArchiveBranch;

public interface IArchiveBranchHandler
{
    Task<ArchiveBranchResult> HandleAsync(
        ArchiveBranchCommand command,
        CancellationToken cancellationToken = default);
}
