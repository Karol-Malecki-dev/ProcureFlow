namespace Application.Modules.Organization.Branch.UpdateBranch;

public interface IUpdateBranchHandler
{
    Task<UpdateBranchResult> HandleAsync(
        UpdateBranchCommand command,
        CancellationToken cancellationToken = default);
}
