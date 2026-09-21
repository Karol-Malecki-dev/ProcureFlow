using FluentValidation;

namespace API.Modules.PurchaseRequests.RemovePurchaseRequestItem;

public sealed class RemovePurchaseRequestItemValidator
    : AbstractValidator<RemovePurchaseRequestItemRequest>
{
    public RemovePurchaseRequestItemValidator()
    {
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);
    }
}
