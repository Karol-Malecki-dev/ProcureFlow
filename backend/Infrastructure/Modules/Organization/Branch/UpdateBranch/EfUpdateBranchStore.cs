using DomainBranch = Domain.Models.Organizations.Branch;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Application.Modules.Organization.Branch.UpdateBranch;

namespace Infrastructure.Modules.Organization.Branch.UpdateBranch;

public sealed class EfUpdateBranchStore : IUpdateBranchStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfUpdateBranchStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainBranch?> GetOwnedBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
        => _dbContext.Branches
            .SingleOrDefaultAsync(
                branch => branch.Id == branchId
                    && branch.OrganizationId == organizationId,
                cancellationToken);

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid branchId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        return _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.OrganizationId == organizationId
                    && branch.Id != branchId
                    && branch.Code == normalizedCode,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        Guid branchId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.OrganizationId == organizationId
                    && branch.Id != branchId
                    && branch.Name == normalizedName,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
