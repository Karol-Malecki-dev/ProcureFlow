using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.Fulfillment;

/// <summary>Validates the delivery transition payload.</summary>
public sealed class MarkPurchaseRequestDeliveredRequestValidator
    : AbstractValidator<MarkPurchaseRequestDeliveredRequest>
{
    public MarkPurchaseRequestDeliveredRequestValidator()
    {
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.FulfillmentNote)
            .MaximumLength(PurchaseRequest.FulfillmentNoteMaxLength)
            .When(request => request.FulfillmentNote is not null);
    }
}
