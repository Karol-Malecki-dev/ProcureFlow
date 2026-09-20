namespace Application.Modules.Organization.Branch.GetBranchDetails;

public enum GetBranchDetailsStatus
{
    Success,
    NotFound
}

public sealed record GetBranchDetailsResult(
    GetBranchDetailsStatus Status,
    BranchDetails? Value = null,
    string Message = "Success")
{
    public bool IsSuccess => Status == GetBranchDetailsStatus.Success;

    public static GetBranchDetailsResult Success(BranchDetails value)
        => new(GetBranchDetailsStatus.Success, value);

    public static GetBranchDetailsResult NotFound(string message)
        => new(GetBranchDetailsStatus.NotFound, null, message);
}
