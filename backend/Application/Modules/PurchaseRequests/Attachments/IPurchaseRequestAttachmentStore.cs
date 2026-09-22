using Domain.Entities;

namespace Application.Modules.PurchaseRequests.Attachments;

/// <summary>Persistence port for purchase-request attachment use cases.</summary>
public interface IPurchaseRequestAttachmentStore
{
    Task<PurchaseRequest?> GetRequestAsync(
        Guid organizationId,
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequestAttachmentView>> ListAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestAttachment?> GetAttachmentAsync(
        Guid purchaseRequestId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<PurchaseRequestAttachmentView> CreateAsync(
        PurchaseRequestAttachment attachment,
        int maxCount,
        long maxBytes,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        PurchaseRequestAttachment attachment,
        CancellationToken cancellationToken = default);

    void ClearChangeTracker();
}

/// <summary>Raised when a request-level attachment quota would be exceeded.</summary>
public sealed class PurchaseRequestAttachmentQuotaExceededException : Exception
{
    public PurchaseRequestAttachmentQuotaExceededException(string message)
        : base(message)
    {
    }
}
