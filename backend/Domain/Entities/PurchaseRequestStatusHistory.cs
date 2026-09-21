using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Immutable audit record for one purchase-request lifecycle transition.
/// </summary>
public sealed class PurchaseRequestStatusHistory
{
    private PurchaseRequestStatusHistory()
    {
    }

    private PurchaseRequestStatusHistory(
        Guid purchaseRequestId,
        PurchaseRequestStatus fromStatus,
        PurchaseRequestStatus toStatus,
        Guid changedByUserId,
        DateTime changedAt)
    {
        Id = Guid.NewGuid();
        PurchaseRequestId = RequireIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = RequireIdentifier(changedByUserId, nameof(changedByUserId));
        ChangedAt = NormalizeUtc(changedAt);
    }

    /// <summary>Unique identifier of this history entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Purchase request whose lifecycle changed.</summary>
    public Guid PurchaseRequestId { get; private set; }

    /// <summary>Status before the transition.</summary>
    public PurchaseRequestStatus FromStatus { get; private set; }

    /// <summary>Status after the transition.</summary>
    public PurchaseRequestStatus ToStatus { get; private set; }

    /// <summary>User who performed the transition.</summary>
    public Guid ChangedByUserId { get; private set; }

    /// <summary>UTC timestamp of the transition.</summary>
    public DateTime ChangedAt { get; private set; }

    /// <summary>Creates one immutable lifecycle transition record.</summary>
    public static PurchaseRequestStatusHistory Create(
        Guid purchaseRequestId,
        PurchaseRequestStatus fromStatus,
        PurchaseRequestStatus toStatus,
        Guid changedByUserId,
        DateTime? changedAtUtc = null)
    {
        if (fromStatus == toStatus)
        {
            throw new ArgumentException("A status history entry must represent a status change.", nameof(toStatus));
        }

        return new PurchaseRequestStatusHistory(
            purchaseRequestId,
            fromStatus,
            toStatus,
            changedByUserId,
            changedAtUtc ?? DateTime.UtcNow);
    }

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}