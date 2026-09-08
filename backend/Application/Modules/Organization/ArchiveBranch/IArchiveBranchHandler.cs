namespace Application.Modules.Organization.ArchiveBranch;

public interface IArchiveBranchHandler
{
    Task<ArchiveBranchResult> HandleAsync(
        ArchiveBranchCommand command,
        CancellationToken cancellationToken = default);
}
