namespace Application.Modules.Catalog.ProductRead;

/// <summary>
/// Focused catalog read port used by purchase-request item creation.
/// </summary>
public interface ISelectableProductReader
{
    Task<SelectableProductView?> GetSelectableProductAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SelectableProductView>> GetSelectableProductsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
