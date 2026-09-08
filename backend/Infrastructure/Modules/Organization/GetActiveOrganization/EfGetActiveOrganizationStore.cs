using Application.Modules.Organization.GetActiveOrganization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.GetActiveOrganization;

public sealed class EfGetActiveOrganizationStore : IGetActiveOrganizationStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfGetActiveOrganizationStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ActiveOrganization?> QueryAsync(
        GetActiveOrganizationQuery query,
        CancellationToken cancellationToken = default)
        => _dbContext.Organizations
            .AsNoTracking()
            .Where(organization => !organization.IsArchived)
            .Select(organization => new ActiveOrganization(
                organization.Id,
                organization.Name,
                organization.Code))
            .SingleOrDefaultAsync(cancellationToken);
}
