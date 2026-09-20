namespace API.Modules.Organization.Branch.UpdateBranch;

public sealed record UpdateBranchRequest(
    string Name,
    string Code,
    UpdateBranchAddressRequest Address);

public sealed record UpdateBranchAddressRequest(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);
