using Domain.Entities;
using FluentValidation;

namespace API.Modules.PurchaseRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestValidator
    : AbstractValidator<CreatePurchaseRequestRequest>
{
    public CreatePurchaseRequestValidator()
    {
        RuleFor(request => request.Note)
            .MaximumLength(PurchaseRequest.NoteMaxLength)
            .When(request => request.Note is not null);
    }
}
