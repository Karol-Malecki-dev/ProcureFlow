using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.Approval;

/// <summary>Validates the optimistic-concurrency token and rejection reason.</summary>
public sealed class DecidePurchaseRequestRequestValidator
    : AbstractValidator<DecidePurchaseRequestRequest>
{
    public DecidePurchaseRequestRequestValidator()
    {
        RuleFor(request => request.ConcurrencyStamp)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(request => request.RejectionReason)
            .NotEmpty()
            .MaximumLength(PurchaseRequest.DecisionReasonMaxLength)
            .When(request => !request.Approve);

        RuleFor(request => request.RejectionReason)
            .Empty()
            .When(request => request.Approve);
    }
}
