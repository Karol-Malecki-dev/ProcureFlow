namespace Application.Modules.Organization.CreateBranch;

public enum BranchOperationStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationError,
    Forbidden
}

public sealed record BranchOperationResult<T>(
    BranchOperationStatus Status,
    T? Value = default,
    string Message = "Success",
    int CreatedStatusCode = 200)
{
    public bool IsSuccess => Status == BranchOperationStatus.Success;

    public static BranchOperationResult<T> Success(
        T value,
        string message = "Success",
        int statusCode = 200)
        => new(BranchOperationStatus.Success, value, message, statusCode);

    public static BranchOperationResult<T> Failure(
        BranchOperationStatus status,
        string message)
        => new(status, default, message);
}
