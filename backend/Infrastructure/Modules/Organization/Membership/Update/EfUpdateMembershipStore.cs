using Application.Modules.Organization.Membership.Update;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.Update;

public sealed class EfUpdateMembershipStore : IUpdateMembershipStore
{
    private readonly ApplicationDbContext _dbContext;
    public EfUpdateMembershipStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DomainOrganization?> GetOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                organization => organization.Id == organizationId,
                cancellationToken);
    }

    public Task<DomainBranch?> GetActiveBranchAsync(Guid organizationId, Guid branchId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Branches
            .AsNoTracking()
            .SingleOrDefaultAsync(
            b => b.OrganizationId == organizationId && 
            b.Id == branchId && 
            !b.IsArchived, cancellationToken);
    }

    public Task<DomainMembership?> GetMembershipForUpdateAsync(Guid organizationId, Guid membershipId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Memberships
            .SingleOrDefaultAsync(
                membership => membership.OrganizationId == organizationId
                    && membership.Id == membershipId,
                cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    => _dbContext.SaveChangesAsync(cancellationToken);
}
