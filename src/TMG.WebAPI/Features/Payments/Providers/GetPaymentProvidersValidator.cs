using TMG.Contracts.Payments;
using FluentValidation;

namespace TMG.WebAPI.Features.Payments.Providers;

public sealed class GetPaymentProvidersValidator : AbstractValidator<GetPaymentProvidersRequest>
{
    public GetPaymentProvidersValidator()
    {
        RuleFor(request => request.Intent)
            .Must(intent => string.IsNullOrWhiteSpace(intent) || Enum.TryParse<PaymentIntent>(intent, true, out _))
            .WithMessage("Intent must be a known payment intent (e.g. RentPayment).");
    }
}
