namespace Application.Modules.Organization.Branch.UpdateBranch;

public enum UpdateBranchStatus
{
    Success,
    NotFound,
    Conflict,
    ValidationError
}

public sealed record UpdateBranchResult(
    UpdateBranchStatus Status,
    UpdatedBranch? Value = null,
    string Message = "Success")
{
    public bool IsSuccess => Status == UpdateBranchStatus.Success;

    public static UpdateBranchResult Success(
        UpdatedBranch value,
        string message = "Branch updated")
        => new(UpdateBranchStatus.Success, value, message);

    public static UpdateBranchResult Failure(
        UpdateBranchStatus status,
        string message)
        => new(status, null, message);
}
