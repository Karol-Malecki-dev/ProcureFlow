using Application.Modules.PurchaseRequests;
using Domain.Enums;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.PurchaseRequests.PurchaseRequestMembershipReader;

/// <summary>
/// Resolves the current user's active membership and the live organization/branch state.
/// </summary>
public sealed class EfPurchaseRequestMembershipReader : IPurchaseRequestMembershipReader
{
    private readonly ApplicationDbContext _dbContext;

    public EfPurchaseRequestMembershipReader(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<PurchaseRequestMembership?> GetCurrentMembershipAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
        => _dbContext.Memberships
            .AsNoTracking()
            .Where(membership => membership.UserId == userId
                && membership.OrganizationId == organizationId
                && membership.IsActive)
            .Select(membership => new PurchaseRequestMembership(
                membership.Id,
                membership.UserId,
                membership.OrganizationId,
                membership.BranchId,
                membership.Role,
                _dbContext.Users.Any(user =>
                    user.Id == membership.UserId
                    && user.IsActive),
                _dbContext.Organizations.Any(organization =>
                    organization.Id == membership.OrganizationId
                    && !organization.IsArchived),
                membership.BranchId.HasValue
                    && _dbContext.Branches.Any(branch =>
                        branch.Id == membership.BranchId.Value
                        && branch.OrganizationId == membership.OrganizationId
                        && !branch.IsArchived),
                _dbContext.Users.Any(user =>
                    user.Id == membership.UserId
                    && user.Role == UserRole.Admin)))
            .SingleOrDefaultAsync(cancellationToken);
}
