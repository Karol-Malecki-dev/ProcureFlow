namespace API.Modules.Organization.CreateBranch
{
    public sealed record CreateBranchRequest(
        string Name,
    string Code,
    CreateBranchAddressRequest Address);
}
