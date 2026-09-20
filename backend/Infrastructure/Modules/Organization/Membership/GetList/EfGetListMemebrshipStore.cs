using Application.Modules.Organization.Membership;
using Application.Modules.Organization.Membership.GetList;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Organization.Membership.GetList;

public sealed class EfListMembershipStore : IListMembershipStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfListMembershipStore(ApplicationDbContext dbContext)
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

    public async Task<IReadOnlyList<MembershipListItem>> QueryAsync(
        ListMembershipsQuery query,
        CancellationToken cancellationToken = default)
    {
        var membershipsQuery =
            from membership in _dbContext.Memberships.AsNoTracking()
            join user in _dbContext.Users
                on membership.UserId equals user.Id
            join branch in _dbContext.Branches
                on membership.BranchId equals (Guid?)branch.Id into branchGroup
            from branch in branchGroup.DefaultIfEmpty()
            where membership.OrganizationId == query.OrganizationId
                && (!query.BranchId.HasValue || membership.BranchId == query.BranchId)
                && (!query.Role.HasValue || membership.Role == query.Role.Value)
                && (query.IncludeInactive || membership.IsActive)
            orderby user.DisplayName.Value, user.Email.Value, membership.Id
            select new MembershipListItem(
                membership.Id,
                membership.UserId,
                user.DisplayName.Value,
                user.Email.Value,
                membership.BranchId,
                branch == null ? null : branch.Name,
                membership.Role,
                membership.IsActive);

        return await membershipsQuery.ToListAsync(cancellationToken);
    }
}
