namespace API.Modules.Organization.Branch.GetBranchDetails;

public sealed record GetBranchDetailsResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    GetBranchDetailsAddressResponse Address);

public sealed record GetBranchDetailsAddressResponse(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);
