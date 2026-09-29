using Application.Modules.Workspace.SearchWorkspace;
using Domain.Enums;
using Domain.Models.Organizations.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Workspace.SearchWorkspace;

public sealed class EfSearchWorkspaceStore : ISearchWorkspaceStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfSearchWorkspaceStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WorkspaceSearchPage> SearchAsync(SearchWorkspaceQuery query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Query.Trim();
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 20);

        if (!string.Equals(query.Type, "purchaseRequest", StringComparison.OrdinalIgnoreCase))
        {
            return new WorkspaceSearchPage([], page, pageSize, 0);
        }

        var purchaseRequests = _dbContext.PurchaseRequests
            .AsNoTracking()
            .Where(request =>
                _dbContext.Organizations.Any(organization =>
                    organization.Id == request.OrganizationId
                    && !organization.IsArchived)
                && _dbContext.Branches.Any(branch =>
                    branch.Id == request.BranchId
                    && branch.OrganizationId == request.OrganizationId
                    && !branch.IsArchived)
                && (
                    _dbContext.Users.Any(user =>
                        user.Id == query.UserId
                        && user.Role == UserRole.Admin)
                    || _dbContext.Memberships.Any(membership =>
                        membership.UserId == query.UserId
                        && membership.OrganizationId == request.OrganizationId
                        && membership.IsActive
                        && (
                            membership.Role == BusinessRole.Procurement
                            || (membership.Role == BusinessRole.Manager
                                && membership.BranchId == request.BranchId)
                            || (membership.Role == BusinessRole.Employee
                                && membership.BranchId == request.BranchId
                                && request.AuthorUserId == query.UserId))))
                && (
                    (request.Note != null && EF.Functions.ILike(request.Note, $"%{normalizedQuery}%"))
                    || request.Items.Any(item =>
                        EF.Functions.ILike(item.ProductNameSnapshot, $"%{normalizedQuery}%")
                        || (item.ProductCodeSnapshot != null
                            && EF.Functions.ILike(item.ProductCodeSnapshot, $"%{normalizedQuery}%")))));

        var totalCount = await purchaseRequests.CountAsync(cancellationToken);
        var items = await purchaseRequests
            .OrderByDescending(request => request.UpdatedAt)
            .ThenByDescending(request => request.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(request => new WorkspaceSearchResult(
                "purchaseRequest",
                request.Id,
                request.Note ?? "Purchase request",
                string.Empty))
            .ToListAsync(cancellationToken);

        return new WorkspaceSearchPage(items, page, pageSize, totalCount);
    }
}
