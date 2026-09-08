namespace Application.Modules.Organization.CreateBranch;

public interface ICreateBranchHandler
{
    Task<BranchOperationResult<BranchView>> HandleAsync(
        CreateBranchCommand command,
        CancellationToken cancellationToken = default);
}
