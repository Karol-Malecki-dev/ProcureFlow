using System.Net;
using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Branch.CreateBranch;

public sealed record BranchOperationResult<T>(
    BranchOperationStatus Status,
    T? Value = default,
    string Message = "Success",
    HttpStatusCode StatusCode = HttpStatusCode.OK)
{
    public bool IsSuccess => Status == BranchOperationStatus.Success;

    public static BranchOperationResult<T> Success(
        T value,
        string message = "Success",
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(BranchOperationStatus.Success, value, message, statusCode);

    public static BranchOperationResult<T> Failure(
        BranchOperationStatus status,
        string message,
        HttpStatusCode? statusCode = null)
        => new(status, default, message, statusCode ?? GetFailureStatusCode(status));

    private static HttpStatusCode GetFailureStatusCode(BranchOperationStatus status) => status switch
    {
        BranchOperationStatus.NotFound => HttpStatusCode.NotFound,
        BranchOperationStatus.Conflict => HttpStatusCode.Conflict,
        BranchOperationStatus.ValidationError => HttpStatusCode.BadRequest,
        BranchOperationStatus.Forbidden => HttpStatusCode.Forbidden,
        _ => HttpStatusCode.InternalServerError
    };
}