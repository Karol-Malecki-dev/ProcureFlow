namespace Application.Modules.Organization.ListBranches;

public interface IListBranchesHandler
{
    Task<ListBranchesResult> HandleAsync(
        ListBranchesQuery query,
        CancellationToken cancellationToken = default);
}
