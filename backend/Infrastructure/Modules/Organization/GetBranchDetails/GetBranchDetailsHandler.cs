using Application.Modules.Organization.GetBranchDetails;

namespace Infrastructure.Modules.Organization.GetBranchDetails;

public sealed class GetBranchDetailsHandler : IGetBranchDetailsHandler
{
    private readonly IGetBranchDetailsStore _store;

    public GetBranchDetailsHandler(IGetBranchDetailsStore store)
    {
        _store = store;
    }

    public async Task<GetBranchDetailsResult> HandleAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var branch = await _store.QueryAsync(query, cancellationToken);

        return branch is null
            ? GetBranchDetailsResult.NotFound("Branch not found.")
            : GetBranchDetailsResult.Success(branch);
    }
}
