namespace API.Modules.Organization.CreateBranch;

public sealed record CreateBranchAddressRequest(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);