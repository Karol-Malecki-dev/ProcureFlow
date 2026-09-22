using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Fulfillment.ListPurchaseRequestFulfillmentQueue;

namespace Infrastructure.Modules.PurchaseRequests.Fulfillment;

/// <summary>Returns accepted requests that still require Procurement work.</summary>
public sealed class ListPurchaseRequestFulfillmentQueueHandler
    : IListPurchaseRequestFulfillmentQueueHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestFulfillmentStore _store;

    public ListPurchaseRequestFulfillmentQueueHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestFulfillmentStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestFulfillmentQueueView>> HandleAsync(
        ListPurchaseRequestFulfillmentQueueQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty || query.OrganizationId == Guid.Empty)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Fulfillment queue identifiers are required.");
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

        if (!PurchaseRequestFulfillmentAccess.CanManage(membership))
        {
            return Failure(
                PurchaseRequestFulfillmentAccess.DeniedStatus(membership),
                "The current user cannot access the Procurement fulfillment queue.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestFulfillmentQueueView>.Success(
            await _store.QueryQueueAsync(membership, cancellationToken));
    }

    private static PurchaseRequestOperationResult<PurchaseRequestFulfillmentQueueView> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestFulfillmentQueueView>.Failure(status, message);
}
