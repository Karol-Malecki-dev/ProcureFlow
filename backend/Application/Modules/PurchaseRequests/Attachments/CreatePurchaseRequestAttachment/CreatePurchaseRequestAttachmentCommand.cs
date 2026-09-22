namespace Application.Modules.PurchaseRequests.Attachments.CreatePurchaseRequestAttachment;

/// <summary>Uploads one validated binary to a purchase request.</summary>
public sealed record CreatePurchaseRequestAttachmentCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Stream Content);

public interface ICreatePurchaseRequestAttachmentHandler
{
    Task<PurchaseRequestOperationResult<PurchaseRequestAttachmentView>> HandleAsync(
        CreatePurchaseRequestAttachmentCommand command,
        CancellationToken cancellationToken = default);
}
