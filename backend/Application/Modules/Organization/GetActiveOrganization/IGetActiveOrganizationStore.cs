namespace Application.Modules.Organization.GetActiveOrganization;

public interface IGetActiveOrganizationStore
{
    Task<ActiveOrganization?> QueryAsync(
        GetActiveOrganizationQuery query,
        CancellationToken cancellationToken = default);
}
