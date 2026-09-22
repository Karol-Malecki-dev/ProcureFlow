namespace Domain.Entities;

/// <summary>
/// Durable request to remove a purchase-request attachment binary.
/// </summary>
public sealed class PurchaseRequestAttachmentCleanupMessage
{
    private PurchaseRequestAttachmentCleanupMessage()
    {
    }

    private PurchaseRequestAttachmentCleanupMessage(
        string storedFileName,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        StoredFileName = string.IsNullOrWhiteSpace(storedFileName)
            ? throw new ArgumentException("Stored file name is required.", nameof(storedFileName))
            : storedFileName.Trim();
        CreatedAt = createdAtUtc.Kind == DateTimeKind.Utc
            ? createdAtUtc
            : createdAtUtc.ToUniversalTime();
        NextAttemptAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string StoredFileName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? LastError { get; private set; }

    public static PurchaseRequestAttachmentCleanupMessage Create(
        string storedFileName,
        DateTime? createdAtUtc = null)
        => new(storedFileName, createdAtUtc ?? DateTime.UtcNow);

    public void MarkProcessed(DateTime processedAtUtc)
    {
        ProcessedAt = processedAtUtc.Kind == DateTimeKind.Utc
            ? processedAtUtc
            : processedAtUtc.ToUniversalTime();
        LastError = null;
    }

    public void MarkFailed(
        string error,
        DateTime nextAttemptAtUtc)
    {
        AttemptCount++;
        var normalizedError = string.IsNullOrWhiteSpace(error)
            ? "Attachment binary cleanup failed."
            : error.Trim();
        LastError = normalizedError[..Math.Min(normalizedError.Length, 2000)];
        NextAttemptAt = nextAttemptAtUtc.Kind == DateTimeKind.Utc
            ? nextAttemptAtUtc
            : nextAttemptAtUtc.ToUniversalTime();
    }
}
