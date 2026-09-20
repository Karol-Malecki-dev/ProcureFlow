namespace API.Modules.Organization.Branch.ListBranches;

public sealed record BranchListItemResponse(
    Guid Id,
    string Name,
    string Code,
    bool IsArchived,
    BranchListAddressResponse Address);

public sealed record BranchListAddressResponse(
    string Street,
    string BuildingNumber,
    string? ApartmentNumber,
    string City,
    string PostalCode,
    string Country);
