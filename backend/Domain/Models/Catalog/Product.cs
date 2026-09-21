namespace Domain.Models.Catalog;

/// <summary>
/// Organization-scoped catalog product used as the source of purchase-request snapshots.
/// </summary>
public sealed class Product
{
    public const int NameMaxLength = 200;
    public const int CodeMaxLength = 100;
    public const int MoneyScale = 2;
    public const decimal MaximumUnitPrice = 9_999_999_999.99m;

    private Product()
    {
    }

    /// <summary>
    /// Creates an active and available catalog product.
    /// </summary>
    public Product(
        Guid organizationId,
        string name,
        string? code,
        Guid unitOfMeasureId,
        decimal unitPrice,
        Guid createdByUserId,
        DateTime? createdAtUtc = null)
    {
        OrganizationId = RequireIdentifier(organizationId, nameof(organizationId));
        UnitOfMeasureId = RequireIdentifier(unitOfMeasureId, nameof(unitOfMeasureId));
        CreatedByUserId = RequireIdentifier(createdByUserId, nameof(createdByUserId));
        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Code = NormalizeCode(code);
        UnitPrice = NormalizeUnitPrice(unitPrice);
        IsActive = true;
        IsAvailable = true;
        CreatedAt = NormalizeUtc(createdAtUtc ?? DateTime.UtcNow);
        UpdatedAt = CreatedAt;
        UpdatedByUserId = createdByUserId;
    }

    /// <summary>Unique identifier of the product.</summary>
    public Guid Id { get; private set; }

    /// <summary>Organization that owns the product.</summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>Product display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Optional organization-scoped catalog code.</summary>
    public string? Code { get; private set; }

    /// <summary>Unit-of-measure reference assigned to the product.</summary>
    public Guid UnitOfMeasureId { get; private set; }

    /// <summary>Current indicative unit price.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Whether the product is available for new request lines.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Whether the product is active in the catalog.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp when the product was last changed.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>User who created the product.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>User who last changed the product.</summary>
    public Guid UpdatedByUserId { get; private set; }

    /// <summary>Archives the product and prevents new request lines from using it.</summary>
    public void Archive(Guid updatedByUserId)
    {
        UpdatedByUserId = RequireIdentifier(updatedByUserId, nameof(updatedByUserId));
        IsActive = false;
        IsAvailable = false;
        Touch();
    }

    /// <summary>Changes whether an active product can be selected in new requests.</summary>
    public void SetAvailability(bool isAvailable, Guid updatedByUserId)
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("An archived product cannot change availability.");
        }

        UpdatedByUserId = RequireIdentifier(updatedByUserId, nameof(updatedByUserId));
        IsAvailable = isAvailable;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentException($"Product name cannot exceed {NameMaxLength} characters.", nameof(name));
        }

        return normalized;
    }

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim();
        if (normalized.Length > CodeMaxLength)
        {
            throw new ArgumentException($"Product code cannot exceed {CodeMaxLength} characters.", nameof(code));
        }

        return normalized;
    }

    private static decimal NormalizeUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0 || unitPrice > MaximumUnitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                $"Unit price must be between zero and {MaximumUnitPrice}.");
        }

        if (decimal.Round(unitPrice, MoneyScale) != unitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                $"Unit price cannot have more than {MoneyScale} decimal places.");
        }

        return unitPrice;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
