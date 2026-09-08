namespace Application.Modules.Organization.UpdateBranch;

public interface IUpdateBranchHandler
{
    Task<UpdateBranchResult> HandleAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default);
}
