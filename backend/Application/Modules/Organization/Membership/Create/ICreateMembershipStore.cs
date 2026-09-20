
using Domain.Entities;
using DomainBranch = Domain.Models.Organizations.Branch;
using DomainMembership = Domain.Models.Organizations.Membership;
using DomainOrganization = Domain.Models.Organizations.Organization;

namespace Application.Modules.Organization.Membership.Create
{
    public interface ICreateMembershipStore
    {
        Task<DomainOrganization?> GetOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

        Task<User?> GetActiveUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<DomainBranch?> GetActiveBranchAsync(
            Guid organizationId,
            Guid branchId,
            CancellationToken cancellationToken = default);

        Task<bool> ActiveMembershipExistsAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        void Add(DomainMembership membership);

        Task SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
