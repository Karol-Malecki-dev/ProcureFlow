
using Domain.ValueObjects;

namespace Application.Modules.Organization.CreateBranch
{
    public sealed record CreateBranchCommand(
        Guid OrganizationId,
        string Name,
        string Code,
        Address Address);
}
