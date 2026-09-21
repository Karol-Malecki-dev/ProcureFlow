namespace Domain.Models.Catalog;

/// <summary>
/// Organization-scoped reference value used to describe the unit of a catalog product.
/// </summary>
public sealed class UnitOfMeasure
{
    public const int NameMaxLength = 100;
    public const int SymbolMaxLength = 20;

    private UnitOfMeasure()
    {
    }

    /// <summary>
    /// Creates an active unit of measure with normalized display values.
    /// </summary>
    public UnitOfMeasure(
        Guid organizationId,
        string name,
        string symbol,
        Guid createdByUserId,
        DateTime? createdAtUtc = null)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization id cannot be empty.", nameof(organizationId));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Created by user id cannot be empty.", nameof(createdByUserId));
        }

        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        Name = NormalizeName(name);
        Symbol = NormalizeSymbol(symbol);
        IsActive = true;
        CreatedAt = NormalizeUtc(createdAtUtc ?? DateTime.UtcNow);
        UpdatedAt = CreatedAt;
        CreatedByUserId = createdByUserId;
        UpdatedByUserId = createdByUserId;
    }

    /// <summary>Unique identifier of the unit of measure.</summary>
    public Guid Id { get; private set; }

    /// <summary>Organization that owns the reference value.</summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>Human-readable unit name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Normalized unit symbol used by products and read models.</summary>
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>Indicates whether the reference may be assigned to new products.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC timestamp when the reference was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp when the reference was last changed.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>User who created the reference.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>User who last changed the reference.</summary>
    public Guid UpdatedByUserId { get; private set; }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Unit name cannot be null or empty.", nameof(name));
        }

        return name.Trim();
    }

    private static string NormalizeSymbol(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException("Unit symbol cannot be null or empty.", nameof(symbol));
        }

        return symbol.Trim().ToLowerInvariant();
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}