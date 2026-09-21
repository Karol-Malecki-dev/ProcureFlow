namespace Application.Modules.Catalog.UnitOfMeasure;

public enum UnitOfMeasureOperationStatus
{
    Success = 0,
    NotFound = 1,
    Conflict = 2,
    ValidationError = 3,
    Forbidden = 4
}