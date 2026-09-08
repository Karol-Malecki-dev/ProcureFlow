using Application.Modules.Organization.GetBranchDetails;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.GetBranchDetails;

public sealed class EfGetBranchDetailsStore : IGetBranchDetailsStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfGetBranchDetailsStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<BranchDetails?> QueryAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var branchesQuery = _dbContext.Branches
            .AsNoTracking()
            .Where(branch => branch.Id == query.BranchId
                && branch.OrganizationId == query.OrganizationId);

        if (!query.IncludeArchived)
        {
            branchesQuery = branchesQuery.Where(branch => !branch.IsArchived);
        }

        return branchesQuery
            .Select(branch => new BranchDetails(
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
            .SingleOrDefaultAsync(cancellationToken);
    }
}
