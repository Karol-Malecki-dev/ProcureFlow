namespace API.Modules.PurchaseRequests.Fulfillment;

/// <summary>Payload for marking an approved request as ordered.</summary>
public sealed record MarkPurchaseRequestOrderedRequest
{
    /// <summary>Concurrency stamp returned with the current request representation.</summary>
    public required string ConcurrencyStamp { get; init; }

    /// <summary>Optional procurement order number.</summary>
    public string? OrderNumber { get; init; }

    /// <summary>Optional operational note for the fulfillment process.</summary>
    public string? FulfillmentNote { get; init; }
}
