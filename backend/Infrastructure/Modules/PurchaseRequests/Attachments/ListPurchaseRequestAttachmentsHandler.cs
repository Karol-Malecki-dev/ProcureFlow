using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.ListPurchaseRequestAttachments;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>Lists attachments visible in the current request scope.</summary>
public sealed class ListPurchaseRequestAttachmentsHandler
    : IListPurchaseRequestAttachmentsHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestAttachmentStore _store;

    public ListPurchaseRequestAttachmentsHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestAttachmentStore store)
    {
        _membershipReader = membershipReader;
        _store = store;
    }

    public async Task<PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>> HandleAsync(
        ListPurchaseRequestAttachmentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty
            || query.OrganizationId == Guid.Empty
            || query.PurchaseRequestId == Guid.Empty)
        {
            return PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>.Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Attachment identifiers are required.");
        }

        var membership = await _membershipReader.GetCurrentMembershipAsync(
            query.UserId,
            query.OrganizationId,
            cancellationToken);
        if (membership is null)
        {
            return PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Active organization membership was not found.");
        }

        var request = await _store.GetRequestAsync(
            query.OrganizationId,
            query.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>.Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request not found.");
        }

        if (!PurchaseRequestAttachmentAccess.CanRead(
                membership,
                request,
                query.UserId))
        {
            return PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>.Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot access these purchase request attachments.");
        }

        return PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>.Success(
            await _store.ListAsync(query.PurchaseRequestId, cancellationToken));
    }
}
