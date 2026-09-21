using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.AddPurchaseRequestItem;

public sealed class AddPurchaseRequestItemValidator
    : AbstractValidator<AddPurchaseRequestItemRequest>
{
    public AddPurchaseRequestItemValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEmpty();
        RuleFor(request => request.Quantity)
            .GreaterThan(0)
            .LessThanOrEqualTo(PurchaseRequestItem.MaximumQuantity)
            .PrecisionScale(12, PurchaseRequestItem.QuantityScale, ignoreTrailingZeros: true);
        RuleFor(request => request.Comment)
            .MaximumLength(PurchaseRequestItem.CommentMaxLength)
            .When(request => request.Comment is not null);
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);
    }
}
