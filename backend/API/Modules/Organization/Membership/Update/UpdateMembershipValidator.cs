using FluentValidation;

namespace API.Modules.Organization.Membership.Update;

public sealed class UpdateMembershipValidator
    : AbstractValidator<UpdateMembershipRequest>
{
    public UpdateMembershipValidator()
    {
        RuleFor(request => request.BranchId)
            .Must(branchId => !branchId.HasValue || branchId.Value != Guid.Empty)
            .WithMessage("Branch id cannot be empty when provided.");

        RuleFor(request => request.Role)
            .IsInEnum()
            .WithMessage("Invalid role specified.");
    }
}