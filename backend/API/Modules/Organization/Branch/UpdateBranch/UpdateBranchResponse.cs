namespace API.Modules.Organization.Branch.UpdateBranch;

public sealed record UpdateBranchResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    UpdateBranchAddressResponse Address);

public sealed record UpdateBranchAddressResponse(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);
