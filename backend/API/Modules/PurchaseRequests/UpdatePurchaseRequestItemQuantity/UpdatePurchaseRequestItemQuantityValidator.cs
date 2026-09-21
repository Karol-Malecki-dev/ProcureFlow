using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.UpdatePurchaseRequestItemQuantity;

public sealed class UpdatePurchaseRequestItemQuantityValidator
    : AbstractValidator<UpdatePurchaseRequestItemQuantityRequest>
{
    public UpdatePurchaseRequestItemQuantityValidator()
    {
        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(PurchaseRequestItem.MaximumQuantity)
            .PrecisionScale(12, PurchaseRequestItem.QuantityScale, ignoreTrailingZeros: true);
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);
    }
}
