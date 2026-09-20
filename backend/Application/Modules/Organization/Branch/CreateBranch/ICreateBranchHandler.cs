namespace Application.Modules.Organization.Branch.CreateBranch;

public interface ICreateBranchHandler
{
    Task<BranchOperationResult<BranchView>> HandleAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default);
}
