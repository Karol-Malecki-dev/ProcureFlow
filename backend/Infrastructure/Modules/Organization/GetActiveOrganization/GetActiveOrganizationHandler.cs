using Application.Modules.Organization.GetActiveOrganization;

namespace Infrastructure.Modules.Organization.GetActiveOrganization;

public sealed class GetActiveOrganizationHandler : IGetActiveOrganizationHandler
{
    private readonly IGetActiveOrganizationStore _store;

    public GetActiveOrganizationHandler(IGetActiveOrganizationStore store)
    {
        _store = store;
    }

    public async Task<GetActiveOrganizationResult> HandleAsync(
        GetActiveOrganizationQuery query,
        CancellationToken cancellationToken = default)
    {
        var organization = await _store.QueryAsync(query, cancellationToken);

        return organization is null
            ? GetActiveOrganizationResult.NotFound("No active organization was found.")
            : GetActiveOrganizationResult.Success(organization);
    }
}
