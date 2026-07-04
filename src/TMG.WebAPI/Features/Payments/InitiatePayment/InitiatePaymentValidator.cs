using TMG.Contracts.Payments;
using FluentValidation;

namespace TMG.WebAPI.Features.Payments.InitiatePayment;

public sealed class InitiatePaymentValidator : AbstractValidator<InitiatePaymentRequest>
{
    public InitiatePaymentValidator()
    {
        // Rent payments are server-priced from the unit, so the client-sent amount is not required for that intent.
        RuleFor(request => request.Amount)
            .GreaterThan(0)
            .When(request => !IsRentPayment(request));

        RuleFor(request => request.CurrencyId)
            .NotEmpty()
            .When(request => !IsRentPayment(request));

        RuleFor(request => request.PaymentProviderId)
            .NotEmpty();

        RuleFor(request => request.PaymentIntent)
            .NotEmpty()
            .Must(paymentIntent => Enum.TryParse<PaymentIntent>(paymentIntent, true, out _))
            .WithMessage("PaymentIntent must be one of: WalletTopUp, Subscription, RentPayment.");

        RuleFor(request => request.TenancyId)
            .NotNull()
            .When(IsRentPayment)
            .WithMessage("TenancyId is required to pay rent in-app.");
    }

    private static bool IsRentPayment(InitiatePaymentRequest request) =>
        Enum.TryParse<PaymentIntent>(request.PaymentIntent, true, out var intent) && intent == PaymentIntent.RentPayment;
}
