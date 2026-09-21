namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Resolves current organization membership without exposing EF to handlers.
/// </summary>
public interface IPurchaseRequestMembershipReader
{
    Task<PurchaseRequestMembership?> GetCurrentMembershipAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
