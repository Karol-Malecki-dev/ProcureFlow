using Application.Modules.Catalog.UnitOfMeasure;
using Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public sealed class CreateUnitOfMeasureHandler : ICreateUnitOfMeasureHandler
{
    private readonly ICreateUnitOfMeasureStore _store;

    public CreateUnitOfMeasureHandler(ICreateUnitOfMeasureStore store)
    {
        _store = store;
    }

    public async Task<UnitOfMeasureOperationResult<UnitOfMeasureView>> HandleAsync(
        CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OrganizationId == Guid.Empty || command.ActorUserId == Guid.Empty)
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.ValidationError,
                "OrganizationId and ActorUserId are required.");
        }

        if (string.IsNullOrWhiteSpace(command.Name)
            || string.IsNullOrWhiteSpace(command.Symbol))
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.ValidationError,
                "Unit name and symbol are required.");
        }

        var organization = await _store.GetOrganizationAsync(
            command.OrganizationId,
            cancellationToken);

        if (organization is null)
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.NotFound,
                $"Organization '{command.OrganizationId}' does not exist.");
        }

        if (organization.IsArchived)
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.Conflict,
                "Archived organization cannot accept catalog changes.");
        }

        if (!await _store.CanManageCatalogAsync(
                command.OrganizationId,
                command.ActorUserId,
                cancellationToken))
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.Forbidden,
                "The current user cannot manage this organization's catalog.");
        }

        if (await _store.ActiveSymbolExistsAsync(
                command.OrganizationId,
                command.Symbol,
                cancellationToken))
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.Conflict,
                $"Unit symbol '{command.Symbol.Trim()}' already exists in this organization.");
        }

        DomainUnitOfMeasure unitOfMeasure;
        try
        {
            unitOfMeasure = new DomainUnitOfMeasure(
                command.OrganizationId,
                command.Name,
                command.Symbol,
                command.ActorUserId);
        }
        catch (ArgumentException exception)
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.ValidationError,
                exception.Message);
        }

        _store.Add(unitOfMeasure);

        try
        {
            await _store.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            PostgreSqlErrorClassifier.IsUniqueConstraintViolation(
                exception,
                "UX_UnitOfMeasures_Organization_ActiveSymbol"))
        {
            return UnitOfMeasureOperationResult<UnitOfMeasureView>.Failure(
                UnitOfMeasureOperationStatus.Conflict,
                "A unit with the same active symbol already exists in this organization.");
        }

        return UnitOfMeasureOperationResult<UnitOfMeasureView>.Success(
            new UnitOfMeasureView(
                unitOfMeasure.Id,
                unitOfMeasure.OrganizationId,
                unitOfMeasure.Name,
                unitOfMeasure.Symbol,
                unitOfMeasure.IsActive),
            "Unit of measure created");
    }
}