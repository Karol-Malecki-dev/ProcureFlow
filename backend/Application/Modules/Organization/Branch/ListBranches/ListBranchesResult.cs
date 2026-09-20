namespace Application.Modules.Organization.Branch.ListBranches;

public enum ListBranchesStatus
{
    Success,
    NotFound
}

public sealed record ListBranchesResult(
    ListBranchesStatus Status,
    IReadOnlyList<BranchListItem>? Value = null,
    string Message = "Success")
{
    public bool IsSuccess => Status == ListBranchesStatus.Success;

    public static ListBranchesResult Success(
        IReadOnlyList<BranchListItem> value)
        => new(ListBranchesStatus.Success, value);

    public static ListBranchesResult NotFound(string message)
        => new(ListBranchesStatus.NotFound, null, message);
}