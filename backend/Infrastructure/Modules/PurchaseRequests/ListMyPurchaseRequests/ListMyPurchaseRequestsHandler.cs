using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.ListMyPurchaseRequests;

namespace Infrastructure.Modules.PurchaseRequests.ListMyPurchaseRequests;

/// <summary>
/// Lists the current Employee's author-owned requests with stable database paging.
/// </summary>
public sealed class ListMyPurchaseRequestsHandler : IListMyPurchaseRequestsHandler
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IListMyPurchaseRequestsStore _store;

    public ListMyPurchaseRequestsHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IListMyPurchaseRequestsStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestListView>> HandleAsync(
        ListMyPurchaseRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty || query.OrganizationId == Guid.Empty)
        {
            return PurchaseRequestOperationResult<PurchaseRequestListView>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "User and organization identifiers are required.");
        }

        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > MaximumPageSize)
        {
            return PurchaseRequestOperationResult<PurchaseRequestListView>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                $"Page must be at least 1 and page size must be between 1 and {MaximumPageSize}.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(query.UserId, query.OrganizationId, cancellationToken);
        if (membership is null || !membership.IsActiveEmployeeScope)
        {
            return PurchaseRequestOperationResult<PurchaseRequestListView>.Success(
                new PurchaseRequestListView([], query.Page, query.PageSize, 0));
        }

        var result = await _store.QueryAsync(membership, query.Page, query.PageSize, cancellationToken);
        return PurchaseRequestOperationResult<PurchaseRequestListView>.Success(result);
    }
}
