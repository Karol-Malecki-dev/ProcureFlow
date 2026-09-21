using System.Net;

namespace Application.Modules.Catalog.UnitOfMeasure;

public sealed record UnitOfMeasureOperationResult<T>(
    UnitOfMeasureOperationStatus Status,
    T? Value = default,
    string Message = "Success",
    HttpStatusCode StatusCode = HttpStatusCode.OK)
{
    public bool IsSuccess => Status == UnitOfMeasureOperationStatus.Success;

    public static UnitOfMeasureOperationResult<T> Success(
        T value,
        string message = "Success",
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(UnitOfMeasureOperationStatus.Success, value, message, statusCode);

    public static UnitOfMeasureOperationResult<T> Failure(
        UnitOfMeasureOperationStatus status,
        string message,
        HttpStatusCode? statusCode = null)
        => new(status, default, message, statusCode ?? GetFailureStatusCode(status));

    private static HttpStatusCode GetFailureStatusCode(UnitOfMeasureOperationStatus status) => status switch
    {
        UnitOfMeasureOperationStatus.NotFound => HttpStatusCode.NotFound,
        UnitOfMeasureOperationStatus.Conflict => HttpStatusCode.Conflict,
        UnitOfMeasureOperationStatus.ValidationError => HttpStatusCode.BadRequest,
        UnitOfMeasureOperationStatus.Forbidden => HttpStatusCode.Forbidden,
        _ => HttpStatusCode.InternalServerError
    };
}