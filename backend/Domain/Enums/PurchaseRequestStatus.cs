namespace Domain.Enums;

/// <summary>
/// Lifecycle states of a purchase request.
/// </summary>
public enum PurchaseRequestStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Rejected = 4,
    Ordered = 5,
    Delivered = 6,
    Cancelled = 7
}
