using Application.Modules.Organization.Membership.Archive;
using DomainMembership = Domain.Models.Organizations.Membership;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.Archive;

public sealed class EfArchiveMembershipStore : IArchiveMembershipStore
{
    private readonly ApplicationDbContext _dbContext;
    public EfArchiveMembershipStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Task<DomainMembership?> GetMembershipForUpdateAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
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
