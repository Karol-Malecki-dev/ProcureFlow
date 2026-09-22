namespace API.Modules.PurchaseRequests.Attachments;

/// <summary>HTTP response for one purchase-request attachment.</summary>
public sealed record PurchaseRequestAttachmentResponse(
    Guid Id,
    Guid PurchaseRequestId,
    Guid UploadedByUserId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);
