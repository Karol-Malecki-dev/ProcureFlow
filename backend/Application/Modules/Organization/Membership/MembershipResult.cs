using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Membership;

public sealed record MembershipResult<T>(
    MembershipOperationStatus Status,
    T? Value = default,
    string Message = "Success")
{
    public bool IsSuccess => Status == MembershipOperationStatus.Success;

    public static MembershipResult<T> Success(
        T value,
        string message = "Success")
        => new(MembershipOperationStatus.Success, value, message);

    public static MembershipResult<T> Failure(
        MembershipOperationStatus status,
        string message)
        => new(status, default, message);
}
