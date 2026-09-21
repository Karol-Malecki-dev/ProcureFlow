namespace API.Modules.Catalog.UnitOfMeasure;

public sealed record UnitOfMeasureResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Symbol,
    bool IsActive);