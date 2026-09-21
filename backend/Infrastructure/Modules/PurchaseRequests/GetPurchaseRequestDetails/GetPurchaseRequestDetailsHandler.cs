using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.GetPurchaseRequestDetails;

namespace Infrastructure.Modules.PurchaseRequests.GetPurchaseRequestDetails;

/// <summary>
/// Returns only author-owned request details within the current membership scope.
/// </summary>
public sealed class GetPurchaseRequestDetailsHandler : IGetPurchaseRequestDetailsHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IGetPurchaseRequestDetailsStore _store;

    public GetPurchaseRequestDetailsHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IGetPurchaseRequestDetailsStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestDetailsView>> HandleAsync(
        GetPurchaseRequestDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty
            || query.OrganizationId == Guid.Empty
            || query.PurchaseRequestId == Guid.Empty)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "User, organization and purchase request identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(query.UserId, query.OrganizationId, cancellationToken);
        if (membership is null || !membership.IsActiveEmployeeScope)
        {
            return PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request was not found.");
        }

        var details = await _store.QueryAsync(membership, query.PurchaseRequestId, cancellationToken);
        return details is null
            ? PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request was not found.")
            : PurchaseRequestOperationResult<PurchaseRequestDetailsView>.Success(details);
    }
}
