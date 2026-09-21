namespace Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public sealed record CreateUnitOfMeasureCommand(
    Guid OrganizationId,
    Guid ActorUserId,
    string Name,
    string Symbol);