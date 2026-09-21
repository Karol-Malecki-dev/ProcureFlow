using Domain.Models.Organizations.Enums;

namespace Application.Modules.Organization.Branch.CreateBranch;

public sealed record BranchOperationResult<T>(
    BranchOperationStatus Status,
    T? Value = default,
    string Message = "Success")
{
    public bool IsSuccess => Status == BranchOperationStatus.Success;

    public static BranchOperationResult<T> Success(
        T value,
        string message = "Success")
        => new(BranchOperationStatus.Success, value, message);

    public static BranchOperationResult<T> Failure(
        BranchOperationStatus status,
        string message)
        => new(status, default, message);
}