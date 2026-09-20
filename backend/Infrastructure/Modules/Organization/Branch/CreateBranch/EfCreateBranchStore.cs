using DomainBranch = Domain.Models.Organizations.Branch;
using DomainOrganization = Domain.Models.Organizations.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Modules.Organization.Branch.CreateBranch;

namespace Infrastructure.Modules.Organization.Branch.CreateBranch;

public sealed class EfCreateBranchStore : ICreateBranchStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfCreateBranchStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
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
        _dbContext.Branches.Add(branch);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
