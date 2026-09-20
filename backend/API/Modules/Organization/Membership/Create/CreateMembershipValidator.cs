using FluentValidation;

namespace API.Modules.Organization.Membership.Create
{
    public sealed class CreateMembershipValidator : AbstractValidator<CreateMembershipRequest>
    {
        public CreateMembershipValidator()
        {
            RuleFor(request => request.UserId)
                .NotEmpty()
                .WithMessage("User id cannot be empty.");
            RuleFor(request => request.BranchId)
                .Must(branchId => !branchId.HasValue || branchId.Value != Guid.Empty)
                .WithMessage("Branch id cannot be empty when provided.");
            RuleFor(request => request.Role)
                .IsInEnum()
                .WithMessage("Invalid role specified.");
        }

    }
}
