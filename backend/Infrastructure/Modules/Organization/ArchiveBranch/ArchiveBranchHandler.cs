using Application.Modules.Organization.ArchiveBranch;

namespace Infrastructure.Modules.Organization.ArchiveBranch;

public sealed class ArchiveBranchHandler : IArchiveBranchHandler
{
    private readonly IArchiveBranchStore _store;

    public ArchiveBranchHandler(IArchiveBranchStore store)
    {
        _store = store;
    }

    public async Task<ArchiveBranchResult> HandleAsync(
        ArchiveBranchCommand command,
        CancellationToken cancellationToken = default)
    {
        var organization = await _store.GetOrganizationWithBranchesAsync(
            command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return ArchiveBranchResult.NotFound("Organization not found.");
        }

        var branch = organization.Branches
            .SingleOrDefault(candidate => candidate.Id == command.BranchId);

        if (branch is null)
        {
            return ArchiveBranchResult.NotFound("Branch not found.");
        }

        if (branch.IsArchived)
        {
            return ArchiveBranchResult.Success("Branch already archived");
        }

        organization.ArchiveBranch(branch);
        await _store.SaveChangesAsync(cancellationToken);

        return ArchiveBranchResult.Success();
    }
}
