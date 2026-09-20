namespace Application.Modules.Organization.Branch.GetBranchDetails;

public interface IGetBranchDetailsHandler
{
    Task<GetBranchDetailsResult> HandleAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default);
}
