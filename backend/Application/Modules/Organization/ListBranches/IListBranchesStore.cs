namespace Application.Modules.Organization.ListBranches;

public interface IListBranchesStore
{
    Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchListItem>> QueryAsync(
        ListBranchesQuery query,
        CancellationToken cancellationToken = default);
}
