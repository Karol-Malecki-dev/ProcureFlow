namespace Application.Modules.Organization.GetActiveOrganization;

public interface IGetActiveOrganizationHandler
{
    Task<GetActiveOrganizationResult> HandleAsync(
        GetActiveOrganizationQuery query,
        CancellationToken cancellationToken = default);
}
