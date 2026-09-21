using Application.Modules.Catalog.UnitOfMeasure;

namespace Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public interface ICreateUnitOfMeasureHandler
{
    Task<UnitOfMeasureOperationResult<UnitOfMeasureView>> HandleAsync(
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default);
}