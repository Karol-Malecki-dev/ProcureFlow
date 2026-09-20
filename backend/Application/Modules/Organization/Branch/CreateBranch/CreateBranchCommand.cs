using Domain.ValueObjects;

namespace Application.Modules.Organization.Branch.CreateBranch
{
    public sealed record CreateBranchCommand(
        Guid OrganizationId,
        string Name,
        string Code,
        Address Address);
}
