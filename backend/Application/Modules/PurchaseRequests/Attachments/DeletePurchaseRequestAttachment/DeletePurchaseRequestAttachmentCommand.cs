namespace Application.Modules.PurchaseRequests.Attachments.DeletePurchaseRequestAttachment;

/// <summary>Deletes one draft attachment and schedules binary cleanup.</summary>
public sealed record DeletePurchaseRequestAttachmentCommand(
    Guid UserId,
    Guid OrganizationId,
    Guid PurchaseRequestId,
    Guid AttachmentId);

public interface IDeletePurchaseRequestAttachmentHandler
{
    Task<PurchaseRequestOperationResult<bool>> HandleAsync(
        DeletePurchaseRequestAttachmentCommand command,
        CancellationToken cancellationToken = default);
}
