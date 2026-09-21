using FluentValidation;

namespace API.Modules.PurchaseRequests.ChangePurchaseRequestStatus;

public sealed class PurchaseRequestStatusChangeRequestValidator
    : AbstractValidator<PurchaseRequestStatusChangeRequest>
{
    public PurchaseRequestStatusChangeRequestValidator()
    {
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);
    }
}