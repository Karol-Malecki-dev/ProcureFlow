using DomainBranch = Domain.Models.Organizations.Branch;

namespace Application.Modules.Organization.Branch.UpdateBranch;

public interface IUpdateBranchStore
{
    Task<DomainBranch?> GetOwnedBranchAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid branchId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        Guid branchId,
        string name,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
