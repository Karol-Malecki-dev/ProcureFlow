namespace Application.Modules.Organization.Branch.ListBranches;

public interface IListBranchesHandler
{
    Task<ListBranchesResult> HandleAsync(
        ListBranchesQuery query,
        CancellationToken cancellationToken = default);
}
