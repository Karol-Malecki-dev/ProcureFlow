namespace Application.Modules.Organization.GetBranchDetails;

public interface IGetBranchDetailsHandler
{
    Task<GetBranchDetailsResult> HandleAsync(
        GetBranchDetailsQuery query,
        CancellationToken cancellationToken = default);
}
