using System.Net;
using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership;

public sealed record MembershipResult<T>(
    MembershipOperationStatus Status,
    T? Value = default,
    string Message = "Success",
    HttpStatusCode StatusCode = HttpStatusCode.OK)
{
    public bool IsSuccess => Status == MembershipOperationStatus.Success;

    public static MembershipResult<T> Success(
        T value,
        string message = "Success",
        HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(MembershipOperationStatus.Success, value, message, statusCode);

    public static MembershipResult<T> Failure(
        MembershipOperationStatus status,
        string message,
        HttpStatusCode? statusCode = null)
        => new(status, default, message, statusCode ?? GetFailureStatusCode(status));

    private static HttpStatusCode GetFailureStatusCode(MembershipOperationStatus status) => status switch
    {
        MembershipOperationStatus.NotFound => HttpStatusCode.NotFound,
        MembershipOperationStatus.Conflict => HttpStatusCode.Conflict,
        MembershipOperationStatus.ValidationError => HttpStatusCode.BadRequest,
        MembershipOperationStatus.Forbidden => HttpStatusCode.Forbidden,
        _ => HttpStatusCode.InternalServerError
    };
}
