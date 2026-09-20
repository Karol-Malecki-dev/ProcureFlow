using Application.Modules.Organization.Membership.Create;
using Application.Modules.Organization.Membership.Get;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.Get;

public sealed class EfGetMembershipDetailsStore : IGetMembershipDetailsStore    
{

    private readonly ApplicationDbContext _dbContext;
    public EfGetMembershipDetailsStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public Task<MembershipDetails?> GetMembershipDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId && membership.IsActive)
            .Select(membership => new MembershipDetails(
                membership.Id,
                membership.OrganizationId,
                membership.UserId,
                membership.BranchId,
                membership.Role,
                membership.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
