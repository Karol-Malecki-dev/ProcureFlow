using Application.Modules.Organization.ListBranches;

namespace Infrastructure.Modules.Organization.ListBranches;

public sealed class ListBranchesHandler : IListBranchesHandler
{
    private readonly IListBranchesStore _store;

    public ListBranchesHandler(IListBranchesStore store)
    {
        _store = store;
    }

    public async Task<ListBranchesResult> HandleAsync(
        ListBranchesQuery query,
        CancellationToken cancellationToken = default)
    {
        var organizationExists = await _store.OrganizationExistsAsync(
            query.OrganizationId,
            cancellationToken);

        if (!organizationExists)
        {
            return ListBranchesResult.NotFound(
                $"Organization '{query.OrganizationId}' does not exist.");
        }

        var branches = await _store.QueryAsync(query, cancellationToken);
        return ListBranchesResult.Success(branches);
    }
}