using Application.Modules.Organization.CreateBranch;
using DomainBranch = Domain.Models.Organizations.Organization.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.CreateBranch;

public sealed class EfCreateBranchStore : ICreateBranchStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfCreateBranchStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .SingleOrDefaultAsync(
                organization => organization.Id == organizationId,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(
                branch =>
                    branch.OrganizationId == organizationId &&
                    branch.Code == normalizedCode,
                cancellationToken);
    }

        public Task<bool> NameExistsAsync(
            Guid organizationId,
            string name,
            CancellationToken cancellationToken = default)
        {
            var normalizedName = name.Trim();

            return _dbContext.Branches
                .AsNoTracking()
                .AnyAsync(
                    branch =>
                        branch.OrganizationId == organizationId &&
                        branch.Name == normalizedName,
                    cancellationToken);
        }

    public void Add(DomainBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        _dbContext.Branches.Add(branch);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
