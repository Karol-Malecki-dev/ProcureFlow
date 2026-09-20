namespace Application.Modules.Organization.Branch.ArchiveBranch;

public enum ArchiveBranchStatus
{
    Success,
    NotFound,
    Conflict
}

public sealed record ArchiveBranchResult(
    ArchiveBranchStatus Status,
    bool? Value = null,
    string Message = "Success")
{
    public bool IsSuccess => Status == ArchiveBranchStatus.Success;

    public static ArchiveBranchResult Success(
        string message = "Branch archived")
        => new(ArchiveBranchStatus.Success, true, message);

    public static ArchiveBranchResult NotFound(string message)
        => new(ArchiveBranchStatus.NotFound, null, message);

    public static ArchiveBranchResult Conflict(string message)
        => new(ArchiveBranchStatus.Conflict, null, message);
}
