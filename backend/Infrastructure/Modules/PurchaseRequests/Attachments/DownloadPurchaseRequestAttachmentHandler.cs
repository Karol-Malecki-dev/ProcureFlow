using Application.Modules.PurchaseRequests;
using Application.Modules.PurchaseRequests.Attachments;
using Application.Modules.PurchaseRequests.Attachments.DownloadPurchaseRequestAttachment;
using Application.Modules.ProjectTasks.Attachments;

namespace Infrastructure.Modules.PurchaseRequests.Attachments;

/// <summary>Opens one attachment binary after request-scope authorization.</summary>
public sealed class DownloadPurchaseRequestAttachmentHandler
    : IDownloadPurchaseRequestAttachmentHandler
{
    private readonly IPurchaseRequestMembershipReader _membershipReader;
    private readonly IPurchaseRequestAttachmentStore _store;
    private readonly IProjectTaskAttachmentStorage _storage;

    public DownloadPurchaseRequestAttachmentHandler(
        IPurchaseRequestMembershipReader membershipReader,
        IPurchaseRequestAttachmentStore store,
        IProjectTaskAttachmentStorage storage)
    {
        _membershipReader = membershipReader;
        _store = store;
        _storage = storage;
    }

    public async Task<PurchaseRequestOperationResult<PurchaseRequestAttachmentDownload>> HandleAsync(
        DownloadPurchaseRequestAttachmentQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.UserId == Guid.Empty
            || query.OrganizationId == Guid.Empty
            || query.PurchaseRequestId == Guid.Empty
            || query.AttachmentId == Guid.Empty)
        {
            return Failure(
                PurchaseRequestOperationStatus.ValidationError,
                "Attachment identifiers are required.");
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

        var request = await _store.GetRequestAsync(
            query.OrganizationId,
            query.PurchaseRequestId,
            cancellationToken);
        if (request is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request not found.");
        }

        if (!PurchaseRequestAttachmentAccess.CanRead(
                membership,
                request,
                query.UserId))
        {
            return Failure(
                PurchaseRequestOperationStatus.Forbidden,
                "The current user cannot access these purchase request attachments.");
        }

        var attachment = await _store.GetAttachmentAsync(
            query.PurchaseRequestId,
            query.AttachmentId,
            cancellationToken);
        if (attachment is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request attachment not found.");
        }

        var content = await _storage.OpenReadAsync(
            attachment.StoredFileName,
            cancellationToken);
        if (content is null)
        {
            return Failure(
                PurchaseRequestOperationStatus.NotFound,
                "Purchase request attachment binary was not found.");
        }

        return PurchaseRequestOperationResult<PurchaseRequestAttachmentDownload>.Success(
            new PurchaseRequestAttachmentDownload(
                content,
                attachment.OriginalFileName,
                attachment.ContentType));
    }

    private static PurchaseRequestOperationResult<PurchaseRequestAttachmentDownload> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => PurchaseRequestOperationResult<PurchaseRequestAttachmentDownload>.Failure(
            status,
            message);
}
