namespace Application.Modules.PurchaseRequests;

/// <summary>
/// HTTP-independent outcomes used by purchase-request draft use cases.
/// </summary>
public enum PurchaseRequestOperationStatus
{
    Success = 0,
    NotFound = 1,
    Conflict = 2,
    ValidationError = 3,
    Forbidden = 4
}
