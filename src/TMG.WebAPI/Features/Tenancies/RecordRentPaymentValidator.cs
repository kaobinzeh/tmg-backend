using FluentValidation;

namespace TMG.WebAPI.Features.Tenancies;

public sealed class RecordRentPaymentValidator : AbstractValidator<RecordRentPaymentRequest>
{
    public RecordRentPaymentValidator()
    {
        RuleFor(request => request.Amount)
            .GreaterThan(0);

        RuleFor(request => request.Method)
            .IsInEnum();

        RuleFor(request => request.Reference)
            .MaximumLength(200)
            .When(request => request.Reference is not null);
    }
}
