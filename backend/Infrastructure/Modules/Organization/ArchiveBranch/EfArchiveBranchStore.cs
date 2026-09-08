using Application.Modules.Organization.ArchiveBranch;
using DomainOrganization = Domain.Models.Organizations.Organization.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.ArchiveBranch;

public sealed class EfArchiveBranchStore : IArchiveBranchStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfArchiveBranchStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainOrganization?> GetOrganizationWithBranchesAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => _dbContext.Organizations
            .Include(organization => organization.Branches)
            .SingleOrDefaultAsync(
                organization => organization.Id == organizationId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
