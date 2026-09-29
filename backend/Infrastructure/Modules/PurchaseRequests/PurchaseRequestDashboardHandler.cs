using Application.Modules.PurchaseRequests;
using Domain.Models.Organizations.Enums;

namespace Infrastructure.Modules.PurchaseRequests;

/// <summary>Resolves membership scope before executing dashboard projections.</summary>
public sealed class GetPurchaseRequestDashboardHandler : IGetPurchaseRequestDashboardHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestDashboardStore _store;

    public GetPurchaseRequestDashboardHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestDashboardStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDashboardView>> HandleAsync(
        GetPurchaseRequestDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty || query.OrganizationId == Guid.Empty)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Dashboard identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            query.UserId,
            query.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        PurchaseRequestDashboardScope scope;
        if (membership.IsActivePlatformAdminScope || membership.IsActiveProcurementScope)
        {
            scope = new PurchaseRequestDashboardScope(
                membership.UserId,
                membership.OrganizationId,
                null,
                membership.Role,
                true);
        }
        else if (membership.IsActiveManagerScope)
        {
            scope = new PurchaseRequestDashboardScope(
                membership.UserId,
                membership.OrganizationId,
                membership.BranchId,
                membership.Role,
                false);
        }
        else if (membership.IsActiveEmployeeScope)
        {
            scope = new PurchaseRequestDashboardScope(
                membership.UserId,
                membership.OrganizationId,
                membership.BranchId,
                membership.Role,
                false);
        }
        else
        {
            return Failure(
                membership.Role is BusinessRole.Employee or BusinessRole.Manager or BusinessRole.Procurement
                    ? PurchaseRequestOperationStatus.Conflict
                    : PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot access a dashboard in this scope.");
        }

        var now = DateTime.UtcNow;
        var monthStartUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStartUtc = monthStartUtc.AddMonths(1);
        var dashboard = await _store.QueryAsync(
            scope,
            monthStartUtc,
            nextMonthStartUtc,
            cancellationToken);

        return PurchaseRequestOperationResult<PurchaseRequestDashboardView>.Success(dashboard);
    }

    private static PurchaseRequestOperationResult<PurchaseRequestDashboardView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestDashboardView>.Failure(status, message);
}