using Application.Modules.Catalog.UnitOfMeasure;
using Application.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using Domain.ValueObjects;
using Infrastructure.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;
using Moq;
using DomainOrganization = Domain.Models.Organizations.Organization;
using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;

namespace UnitTests.Modules.Catalog.CreateUnitOfMeasure;

public sealed class CreateUnitOfMeasureHandlerTests
{
    private readonly Mock<ICreateUnitOfMeasureStore> _store = new();

    [Fact]
    public async Task Handle_creates_active_unit_for_authorized_catalog_manager()
    {
        var organization = CreateOrganization();
        var actorUserId = Guid.NewGuid();
        var command = new CreateUnitOfMeasureCommand(
            organization.Id,
            actorUserId,
            " Kilogram ",
            " KG ");

        SetupActiveOrganization(command, organization);

        var result = await CreateHandler().HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.Success, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(organization.Id, result.Value!.OrganizationId);
        Assert.Equal("Kilogram", result.Value.Name);
        Assert.Equal("kg", result.Value.Symbol);
        Assert.True(result.Value.IsActive);
        _store.Verify(store => store.Add(It.Is<DomainUnitOfMeasure>(unit =>
            unit.OrganizationId == organization.Id
            && unit.CreatedByUserId == actorUserId
            && unit.Name == "Kilogram"
            && unit.Symbol == "kg"
            && unit.IsActive)), Times.Once);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_returns_not_found_when_organization_does_not_exist()
    {
        var organizationId = Guid.NewGuid();
        var command = new CreateUnitOfMeasureCommand(
            organizationId,
            Guid.NewGuid(),
            "Kilogram",
            "kg");

        _store
            .Setup(store => store.GetOrganizationAsync(
                organizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainOrganization?)null);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.NotFound, result.Status);
        _store.Verify(store => store.CanManageCatalogAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainUnitOfMeasure>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_for_archived_organization()
    {
        var organization = CreateOrganization();
        organization.Archive();
        var command = new CreateUnitOfMeasureCommand(
            organization.Id,
            Guid.NewGuid(),
            "Kilogram",
            "kg");

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.CanManageCatalogAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainUnitOfMeasure>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_forbidden_when_actor_cannot_manage_catalog()
    {
        var organization = CreateOrganization();
        var command = new CreateUnitOfMeasureCommand(
            organization.Id,
            Guid.NewGuid(),
            "Kilogram",
            "kg");

        _store
            .Setup(store => store.GetOrganizationAsync(
                organization.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.CanManageCatalogAsync(
                organization.Id,
                command.ActorUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.Forbidden, result.Status);
        _store.Verify(store => store.ActiveSymbolExistsAsync(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainUnitOfMeasure>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_conflict_when_active_symbol_already_exists()
    {
        var organization = CreateOrganization();
        var command = new CreateUnitOfMeasureCommand(
            organization.Id,
            Guid.NewGuid(),
            "Kilogram",
            " KG ");

        SetupActiveOrganization(command, organization);
        _store
            .Setup(store => store.ActiveSymbolExistsAsync(
                organization.Id,
                command.Symbol,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.Conflict, result.Status);
        _store.Verify(store => store.Add(It.IsAny<DomainUnitOfMeasure>()), Times.Never);
        _store.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_returns_validation_error_for_blank_input()
    {
        var command = new CreateUnitOfMeasureCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " ",
            "kg");

        var result = await CreateHandler().HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(UnitOfMeasureOperationStatus.ValidationError, result.Status);
        _store.Verify(store => store.GetOrganizationAsync(
            It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(store => store.Add(It.IsAny<DomainUnitOfMeasure>()), Times.Never);
    }

    private void SetupActiveOrganization(
        CreateUnitOfMeasureCommand command,
        DomainOrganization organization)
    {
        _store
            .Setup(store => store.GetOrganizationAsync(
                command.OrganizationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
        _store
            .Setup(store => store.CanManageCatalogAsync(
                command.OrganizationId,
                command.ActorUserId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _store
            .Setup(store => store.ActiveSymbolExistsAsync(
                command.OrganizationId,
                command.Symbol,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _store
            .Setup(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CreateUnitOfMeasureHandler CreateHandler()
        => new(_store.Object);

    private static DomainOrganization CreateOrganization()
        => new(
            "ProcureFlow",
            new Address
            {
                Street = "Main Street",
                BuildingNumber = "1",
                City = "Warsaw",
                PostalCode = "00-001",
                Country = "Poland"
            },
            $"PF-{Guid.NewGuid():N}",
            null);
}