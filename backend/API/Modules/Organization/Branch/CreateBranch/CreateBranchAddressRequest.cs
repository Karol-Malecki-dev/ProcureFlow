namespace API.Modules.Organization.Branch.CreateBranch;

public sealed record CreateBranchAddressRequest(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);