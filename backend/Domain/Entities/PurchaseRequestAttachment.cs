namespace Domain.Entities;

/// <summary>
/// Metadata for one file attached to a purchase request.
/// The binary is owned by the configured attachment storage.
/// </summary>
public sealed class PurchaseRequestAttachment
{
    private PurchaseRequestAttachment()
    {
    }

    private PurchaseRequestAttachment(
        Guid purchaseRequestId,
        Guid uploadedByUserId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long sizeBytes,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        PurchaseRequestId = RequireIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        UploadedByUserId = RequireIdentifier(uploadedByUserId, nameof(uploadedByUserId));
        OriginalFileName = RequireText(originalFileName, 255, nameof(originalFileName));
        StoredFileName = RequireText(storedFileName, 100, nameof(storedFileName));
        ContentType = RequireText(contentType, 128, nameof(contentType));

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                sizeBytes,
                "Attachment size must be greater than zero.");
        }

        SizeBytes = sizeBytes;
        CreatedAt = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : createdAtUtc.ToUniversalTime();
    }

    /// <summary>Unique identifier of the attachment metadata.</summary>
    public Guid Id { get; private set; }

    /// <summary>Purchase request that owns the attachment metadata.</summary>
    public Guid PurchaseRequestId { get; private set; }

    /// <summary>User who uploaded the attachment.</summary>
    public Guid UploadedByUserId { get; private set; }

    /// <summary>Safe display name supplied by the uploader.</summary>
    public string OriginalFileName { get; private set; } = string.Empty;

    /// <summary>Generated storage key used to address the binary.</summary>
    public string StoredFileName { get; private set; } = string.Empty;

    /// <summary>Validated media type of the binary.</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Length of the stored binary in bytes.</summary>
    public long SizeBytes { get; private set; }

    /// <summary>UTC timestamp when the metadata was created.</summary>
    public DateTime CreatedAt { get; private set; }

    public static PurchaseRequestAttachment Create(
        Guid purchaseRequestId,
        Guid uploadedByUserId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long sizeBytes,
        DateTime? createdAtUtc = null)
        => new(
            purchaseRequestId,
            uploadedByUserId,
            originalFileName,
            storedFileName,
            contentType,
            sizeBytes,
            createdAtUtc ?? DateTime.UtcNow);

    private static Guid RequireIdentifier(Guid value, string parameterName)
        => value == Guid.Empty
            ? throw new ArgumentException("Identifier is required.", parameterName)
            : value;

    private static string RequireText(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
