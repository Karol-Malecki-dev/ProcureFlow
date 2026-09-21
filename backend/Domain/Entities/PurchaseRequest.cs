using Domain.Enums;

namespace Domain.Entities;

/// <summary>
/// Aggregate root for an employee purchase-request draft.
/// The aggregate owns item mutations, total calculation and draft editability.
/// </summary>
public sealed class PurchaseRequest
{
    public const int NoteMaxLength = 2_000;

    private readonly List<PurchaseRequestItem> _items = [];

    private PurchaseRequest()
    {
    }

    private PurchaseRequest(
        Guid authorUserId,
        Guid organizationId,
        Guid branchId,
        string? note,
        DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        AuthorUserId = RequireIdentifier(authorUserId, nameof(authorUserId));
        OrganizationId = RequireIdentifier(organizationId, nameof(organizationId));
        BranchId = RequireIdentifier(branchId, nameof(branchId));
        Note = NormalizeNote(note);
        Status = PurchaseRequestStatus.Draft;
        TotalValue = 0m;
        CreatedAt = NormalizeUtc(createdAtUtc);
        UpdatedAt = CreatedAt;
        ConcurrencyStamp = GenerateConcurrencyStamp();
    }

    /// <summary>Unique identifier of the purchase request.</summary>
    public Guid Id { get; private set; }

    /// <summary>User who created and owns the request in this branch.</summary>
    public Guid AuthorUserId { get; private set; }

    /// <summary>Organization scope derived from the author's membership.</summary>
    public Guid OrganizationId { get; private set; }

    /// <summary>Branch scope derived from the author's Employee membership.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>Current lifecycle state.</summary>
    public PurchaseRequestStatus Status { get; private set; }

    /// <summary>Optional request-level note.</summary>
    public string? Note { get; private set; }

    /// <summary>Server-calculated total value of all item lines.</summary>
    public decimal TotalValue { get; private set; }

    /// <summary>UTC timestamp when the request was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp when the aggregate last changed.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Optimistic-concurrency version of the aggregate.</summary>
    public string ConcurrencyStamp { get; private set; } = string.Empty;

    /// <summary>Items owned by this aggregate.</summary>
    public IReadOnlyCollection<PurchaseRequestItem> Items => _items;

    /// <summary>
    /// Creates an empty draft. Empty drafts remain valid until the submission branch
    /// applies the completeness rule.
    /// </summary>
    public static PurchaseRequest Create(
        Guid authorUserId,
        Guid organizationId,
        Guid branchId,
        string? note = null,
        DateTime? createdAtUtc = null)
        => new(
            authorUserId,
            organizationId,
            branchId,
            note,
            createdAtUtc ?? DateTime.UtcNow);

    /// <summary>
    /// Adds one server-owned product snapshot to the draft.
    /// </summary>
    public PurchaseRequestItem AddItem(
        Guid productId,
        string productNameSnapshot,
        string? productCodeSnapshot,
        string unitNameSnapshot,
        string unitSymbolSnapshot,
        decimal unitPriceSnapshot,
        decimal quantity,
        string? comment = null)
    {
        EnsureDraft();

        if (_items.Any(item => item.ProductId == productId))
        {
            throw new InvalidOperationException("The product is already present in this purchase request.");
        }

        var item = PurchaseRequestItem.Create(
            Id,
            productId,
            productNameSnapshot,
            productCodeSnapshot,
            unitNameSnapshot,
            unitSymbolSnapshot,
            unitPriceSnapshot,
            quantity,
            comment);

        _items.Add(item);
        RecalculateTotal();
        Touch();
        return item;
    }

    /// <summary>
    /// Changes one item quantity while preserving its historical snapshots.
    /// </summary>
    public void UpdateItemQuantity(Guid itemId, decimal quantity)
    {
        EnsureDraft();
        FindItem(itemId).ChangeQuantity(quantity);
        RecalculateTotal();
        Touch();
    }

    /// <summary>
    /// Removes one item. Removing the last item leaves a valid empty draft.
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        EnsureDraft();
        var item = FindItem(itemId);
        _items.Remove(item);
        RecalculateTotal();
        Touch();
    }

    private PurchaseRequestItem FindItem(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", nameof(itemId));
        }

        return _items.FirstOrDefault(item => item.Id == itemId)
            ?? throw new InvalidOperationException("Purchase request item was not found.");
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseRequestStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase requests can be edited.");
        }
    }

    private void RecalculateTotal()
    {
        TotalValue = decimal.Round(
            _items.Sum(item => item.LineTotal),
            PurchaseRequestItem.MoneyScale,
            MidpointRounding.AwayFromZero);
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
        ConcurrencyStamp = GenerateConcurrencyStamp();
    }

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var normalized = note.Trim();
        if (normalized.Length > NoteMaxLength)
        {
            throw new ArgumentException(
                $"Purchase request note cannot exceed {NoteMaxLength} characters.",
                nameof(note));
        }

        return normalized;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();

    private static string GenerateConcurrencyStamp()
        => Guid.NewGuid().ToString("N");
}
