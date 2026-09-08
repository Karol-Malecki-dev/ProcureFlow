namespace Application.Modules.Organization.GetBranchDetails;

public interface IGetBranchDetailsStore
{
    Task<BranchDetails?> QueryAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default);
}
