namespace Domain.Enums;

/// <summary>Durable outcomes recorded for purchase-request approval decisions.</summary>
public enum PurchaseRequestDecisionType
{
    Approved = 1,
    Rejected = 2,
    Escalated = 3
}
