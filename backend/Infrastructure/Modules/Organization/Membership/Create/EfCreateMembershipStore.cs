using Application.Modules.Organization.Membership.Create;
using Domain.Entities;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.Create;

public sealed class EfCreateMembershipStore : ICreateMembershipStore
{
    private readonly ApplicationDbContext _dbContext;
    public EfCreateMembershipStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<bool> ActiveMembershipExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Memberships
            .AsNoTracking()
            .AnyAsync(membership => membership.UserId == userId && 
            membership.IsActive, 
            cancellationToken);
    }

    public void Add(DomainMembership membership)
    {
        _dbContext.Memberships.Add(membership);
    }

    public async Task<DomainBranch?> GetActiveBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                branch => branch.Id == branchId
                    && branch.OrganizationId == organizationId
                    && !branch.IsArchived,
                cancellationToken);
    }

    public Task<User?> GetActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
    return _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId && user.IsActive, cancellationToken);
    }

    public async Task<DomainOrganization?> GetOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .FindAsync(new object[] { organizationId }, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
