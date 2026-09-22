using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.Fulfillment;

/// <summary>Validates the order transition payload.</summary>
public sealed class MarkPurchaseRequestOrderedRequestValidator
    : AbstractValidator<MarkPurchaseRequestOrderedRequest>
{
    public MarkPurchaseRequestOrderedRequestValidator()
    {
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.OrderNumber)
            .MaximumLength(PurchaseRequest.FulfillmentOrderNumberMaxLength)
            .When(request => request.OrderNumber is not null);

        RuleFor(request => request.FulfillmentNote)
            .MaximumLength(PurchaseRequest.FulfillmentNoteMaxLength)
            .When(request => request.FulfillmentNote is not null);
    }
}
