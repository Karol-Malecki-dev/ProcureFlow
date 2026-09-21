namespace Application.Modules.Catalog.ProductRead;

/// <summary>
/// Server-owned catalog data that may be copied into a purchase-request item snapshot.
/// </summary>
public sealed record SelectableProductView(
    Guid ProductId,
    Guid OrganizationId,
    string Name,
    string? Code,
    string UnitName,
    string UnitSymbol,
    decimal UnitPrice);
