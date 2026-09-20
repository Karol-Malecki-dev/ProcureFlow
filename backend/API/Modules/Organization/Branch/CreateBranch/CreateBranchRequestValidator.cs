using FluentValidation;

namespace API.Modules.Organization.Branch.CreateBranch;

public sealed class CreateBranchRequestValidator
    : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(request => request.Address)
            .NotNull()
            .SetValidator(new CreateBranchAddressRequestValidator());
    }
}
