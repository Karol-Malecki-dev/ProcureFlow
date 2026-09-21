namespace Domain.Entities;

/// <summary>
/// A product line owned by a <see cref="PurchaseRequest"/> aggregate.
/// Product display and pricing fields are snapshots captured when the line is added.
/// </summary>
public sealed class PurchaseRequestItem
{
    public const int ProductNameMaxLength = 200;
    public const int ProductCodeMaxLength = 100;
    public const int UnitNameMaxLength = 100;
    public const int UnitSymbolMaxLength = 20;
    public const int CommentMaxLength = 1_000;
    public const int QuantityScale = 3;
    public const int MoneyScale = 2;
    public const decimal MaximumQuantity = 1_000_000m;
    public const decimal MaximumUnitPrice = 9_999_999_999.99m;

    private PurchaseRequestItem()
    {
    }

    private PurchaseRequestItem(
        Guid purchaseRequestId,
        Guid productId,
        string productNameSnapshot,
        string? productCodeSnapshot,
        string unitNameSnapshot,
        string unitSymbolSnapshot,
        decimal unitPriceSnapshot,
        decimal quantity,
        string? comment)
    {
        Id = Guid.NewGuid();
        PurchaseRequestId = RequireIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        ProductId = RequireIdentifier(productId, nameof(productId));
        ProductNameSnapshot = NormalizeRequiredText(
            productNameSnapshot,
            ProductNameMaxLength,
            nameof(productNameSnapshot));
        ProductCodeSnapshot = NormalizeOptionalText(
            productCodeSnapshot,
            ProductCodeMaxLength,
            nameof(productCodeSnapshot));
        UnitNameSnapshot = NormalizeRequiredText(
            unitNameSnapshot,
            UnitNameMaxLength,
            nameof(unitNameSnapshot));
        UnitSymbolSnapshot = NormalizeSymbol(unitSymbolSnapshot);
        UnitPriceSnapshot = NormalizeUnitPrice(unitPriceSnapshot);
        Quantity = NormalizeQuantity(quantity);
        Comment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
    }

    /// <summary>Unique identifier of the request item.</summary>
    public Guid Id { get; private set; }

    /// <summary>Parent purchase-request identifier.</summary>
    public Guid PurchaseRequestId { get; private set; }

    /// <summary>Catalog product identifier retained for traceability.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Historical product name captured at add time.</summary>
    public string ProductNameSnapshot { get; private set; } = string.Empty;

    /// <summary>Historical optional product code captured at add time.</summary>
    public string? ProductCodeSnapshot { get; private set; }

    /// <summary>Historical unit name captured at add time.</summary>
    public string UnitNameSnapshot { get; private set; } = string.Empty;

    /// <summary>Historical unit symbol captured at add time.</summary>
    public string UnitSymbolSnapshot { get; private set; } = string.Empty;

    /// <summary>Historical unit price captured at add time.</summary>
    public decimal UnitPriceSnapshot { get; private set; }

    /// <summary>Requested quantity.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Optional item-level comment.</summary>
    public string? Comment { get; private set; }

    /// <summary>Line value rounded to the configured money scale.</summary>
    public decimal LineTotal => decimal.Round(
        UnitPriceSnapshot * Quantity,
        MoneyScale,
        MidpointRounding.AwayFromZero);

    internal static PurchaseRequestItem Create(
        Guid purchaseRequestId,
        Guid productId,
        string productNameSnapshot,
        string? productCodeSnapshot,
        string unitNameSnapshot,
        string unitSymbolSnapshot,
        decimal unitPriceSnapshot,
        decimal quantity,
        string? comment)
        => new(
            purchaseRequestId,
            productId,
            productNameSnapshot,
            productCodeSnapshot,
            unitNameSnapshot,
            unitSymbolSnapshot,
            unitPriceSnapshot,
            quantity,
            comment);

    internal void ChangeQuantity(decimal quantity)
    {
        Quantity = NormalizeQuantity(quantity);
    }

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static string NormalizeRequiredText(string value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", parameterName);
        }

        return normalized;
    }

    private static string NormalizeSymbol(string value)
        => NormalizeRequiredText(value, UnitSymbolMaxLength, nameof(value)).ToLowerInvariant();

    private static decimal NormalizeQuantity(decimal value)
    {
        if (value <= 0 || value > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Quantity must be greater than zero and no greater than {MaximumQuantity}.");
        }

        if (decimal.Round(value, QuantityScale) != value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Quantity cannot have more than {QuantityScale} decimal places.");
        }

        return value;
    }

    private static decimal NormalizeUnitPrice(decimal value)
    {
        if (value < 0 || value > MaximumUnitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Unit price must be between zero and {MaximumUnitPrice}.");
        }

        if (decimal.Round(value, MoneyScale) != value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                $"Unit price cannot have more than {MoneyScale} decimal places.");
        }

        return value;
    }
}
