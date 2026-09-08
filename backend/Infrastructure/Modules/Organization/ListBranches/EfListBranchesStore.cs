using Application.Modules.Organization.ListBranches;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.ListBranches;

public sealed class EfListBranchesStore : IListBranchesStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfListBranchesStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(
                organization => organization.Id == organizationId,
                cancellationToken);

    public async Task<IReadOnlyList<BranchListItem>> QueryAsync(
        ListBranchesQuery query,
        CancellationToken cancellationToken = default)
    {
        var branchesQuery = _dbContext.Branches
            .AsNoTracking()
            .Where(branch => branch.OrganizationId == query.OrganizationId);

        if (!query.IncludeArchived)
        {
            branchesQuery = branchesQuery.Where(branch => !branch.IsArchived);
        }

        return await branchesQuery
            .OrderBy(branch => branch.Name)
            .ThenBy(branch => branch.Code)
            .Select(branch => new BranchListItem(
                branch.Id,
                branch.Name,
                branch.Code,
                branch.IsArchived,
                new Domain.ValueObjects.Address
                {
                    Street = branch.Address.Street,
                    BuildingNumber = branch.Address.BuildingNumber,
                    ApartmentNumber = branch.Address.ApartmentNumber,
                    City = branch.Address.City,
                    PostalCode = branch.Address.PostalCode,
                    Country = branch.Address.Country
                }))
            .ToListAsync(cancellationToken);
    }
}