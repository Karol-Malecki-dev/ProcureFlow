using FluentValidation;

namespace API.Modules.Organization.UpdateBranch;

public sealed class UpdateBranchRequestValidator
    : AbstractValidator<UpdateBranchRequest>
{
    public UpdateBranchRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.Address)
            .NotNull()
            .SetValidator(new UpdateBranchAddressRequestValidator());
    }
}

public sealed class UpdateBranchAddressRequestValidator
    : AbstractValidator<UpdateBranchAddressRequest>
{
    public UpdateBranchAddressRequestValidator()
    {
        RuleFor(request => request.Street)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.BuildingNumber)
            .NotEmpty()
            .MaximumLength(30);

        RuleFor(request => request.ApartmentNumber)
            .MaximumLength(30);

        RuleFor(request => request.City)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.PostalCode)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(request => request.Country)
            .NotEmpty()
            .MaximumLength(100);
    }
}
