namespace Application.Modules.PurchaseRequests.Attachments.DownloadPurchaseRequestAttachment;

/// <summary>Downloads one attachment after request-scope authorization.</summary>
public sealed record DownloadPurchaseRequestAttachmentQuery(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    Guid AttachmentId);

public interface IDownloadPurchaseRequestAttachmentHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestAttachmentDownload>> HandleAsync(
        DownloadPurchaseRequestAttachmentQuery query,
        CancellationToken cancellationToken = default);
}
