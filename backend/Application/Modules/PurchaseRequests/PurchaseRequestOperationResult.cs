namespace Application.Modules.PurchaseRequests;

/// <summary>
/// Result returned by a purchase-request application use case.
/// </summary>
public sealed record PurchaseRequestOperationResult<T>(
    PurchaseRequestOperationStatus Status,
    T? Value = default,
    string Message = "Success")
{
    public bool IsSuccess => Status == PurchaseRequestOperationStatus.Success;

    public static PurchaseRequestOperationResult<T> Success(
        T value,
        string message = "Success")
        => new(PurchaseRequestOperationStatus.Success, value, message);

    public static PurchaseRequestOperationResult<T> Failure(
        PurchaseRequestOperationStatus status,
        string message)
        => new(status, default, message);
}
