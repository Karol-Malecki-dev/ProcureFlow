namespace Application.Modules.PurchaseRequests.Attachments;

/// <summary>Read-only projection of one purchase-request attachment.</summary>
public sealed record PurchaseRequestAttachmentView(
    Guid Id,
    Guid PurchaseRequestId,
    Guid UploadedByUserId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTime CreatedAt);

/// <summary>Binary payload and metadata returned by an authorized download.</summary>
public sealed record PurchaseRequestAttachmentDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);
