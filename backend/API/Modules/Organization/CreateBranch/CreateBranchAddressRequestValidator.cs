using FluentValidation;

namespace API.Modules.Organization.CreateBranch;

public sealed class CreateBranchAddressRequestValidator
    : AbstractValidator<CreateBranchAddressRequest>
{
    public CreateBranchAddressRequestValidator()
    {
        RuleFor(address => address.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(address => address.BuildingNumber)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(address => address.ApartmentNumber)
            .MaximumLength(30);

        RuleFor(address => address.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(address => address.PostalCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(address => address.Country)
            .NotEmpty()
            .MaximumLength(100);
    }
}