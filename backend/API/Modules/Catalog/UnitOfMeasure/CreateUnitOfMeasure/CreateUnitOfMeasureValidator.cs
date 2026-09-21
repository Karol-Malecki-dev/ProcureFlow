using DomainUnitOfMeasure = Domain.Models.Catalog.UnitOfMeasure;
using FluentValidation;

namespace API.Modules.Catalog.UnitOfMeasure.CreateUnitOfMeasure;

public sealed class CreateUnitOfMeasureValidator
    : AbstractValidator<CreateUnitOfMeasureRequest>
{
    public CreateUnitOfMeasureValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(DomainUnitOfMeasure.NameMaxLength);

        RuleFor(request => request.Symbol)
            .NotEmpty()
            .MaximumLength(DomainUnitOfMeasure.SymbolMaxLength);
    }
}