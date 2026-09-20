namespace Application.Modules.Organization.Branch.GetBranchDetails;

public interface IGetBranchDetailsStore
{
    Task<BranchDetails?> QueryAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default);
}
