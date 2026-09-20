namespace API.Modules.Organization.Branch.CreateBranch
{
    public sealed record CreateBranchRequest(
    string Name,
    string Code,
    CreateBranchAddressRequest Address);
}
