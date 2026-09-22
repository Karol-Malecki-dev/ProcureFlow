namespace Application.Modules.PurchaseRequests.Attachments;

/// <summary>Processes durable deletion requests for purchase-request binaries.</summary>
public interface IPurchaseRequestAttachmentCleanupProcessor
{
    Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default);
}
