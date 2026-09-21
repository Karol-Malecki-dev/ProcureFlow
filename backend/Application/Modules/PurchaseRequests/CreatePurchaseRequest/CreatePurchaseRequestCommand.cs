namespace Application.Modules.PurchaseRequests.CreatePurchaseRequest;

/// <summary>Input for creating an empty request draft.</summary>
public sealed record CreatePurchaseRequestCommand(
    Guid UserId,
    Guid OrganizationId,
    string? Note);
