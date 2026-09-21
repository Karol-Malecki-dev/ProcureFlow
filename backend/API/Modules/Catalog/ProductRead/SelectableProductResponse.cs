namespace API.Modules.Catalog.ProductRead;

/// <summary>
/// Product data available for adding a new purchase-request item.
/// </summary>
public sealed record SelectableProductResponse(
    Guid Id,
    string Name,
    string? Code,
    string UnitName,
    string UnitSymbol,
    decimal UnitPrice);