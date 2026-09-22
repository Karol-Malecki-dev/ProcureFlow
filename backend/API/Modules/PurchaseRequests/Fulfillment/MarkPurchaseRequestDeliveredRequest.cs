namespace API.Modules.PurchaseRequests.Fulfillment;

/// <summary>Payload for marking an ordered request as delivered.</summary>
public sealed record MarkPurchaseRequestDeliveredRequest
{
    /// <summary>Concurrency stamp returned with the current request representation.</summary>
    public required string ConcurrencyStamp { get; init; }

    /// <summary>Optional final operational note for the delivery.</summary>
    public string? FulfillmentNote { get; init; }
}
