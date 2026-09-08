namespace API.Modules.Organization.CreateBranch;

public sealed record BranchResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    BranchAddressResponse Address);

public sealed record BranchAddressResponse(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);