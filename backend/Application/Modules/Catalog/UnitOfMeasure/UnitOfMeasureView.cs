namespace Application.Modules.Catalog.UnitOfMeasure;

public sealed record UnitOfMeasureView(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Symbol,
    bool IsActive);