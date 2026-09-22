namespace Application.Modules.PurchaseRequests.Attachments.ListPurchaseRequestAttachments;

/// <summary>Lists attachments visible to the current user for one request.</summary>
public sealed record ListPurchaseRequestAttachmentsQuery(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId);

public interface IListPurchaseRequestAttachmentsHandler
{
    Task<PurchaseRequestOperationResult<IReadOnlyList<PurchaseRequestAttachmentView>>> HandleAsync(
        ListPurchaseRequestAttachmentsQuery query,
        CancellationToken cancellationToken = default);
}
