namespace Application.Modules.Catalog.UnitOfMeasure;

public sealed record UnitOfMeasureOperationResult<T>(
    UnitOfMeasureOperationStatus Status,
    T? Value = default,
    string Message = "Success")
{
    public bool IsSuccess => Status == UnitOfMeasureOperationStatus.Success;

    public static UnitOfMeasureOperationResult<T> Success(
        T value,
        string message = "Success")
        => new(UnitOfMeasureOperationStatus.Success, value, message);

    public static UnitOfMeasureOperationResult<T> Failure(
        UnitOfMeasureOperationStatus status,
        string message)
        => new(status, default, message);
}