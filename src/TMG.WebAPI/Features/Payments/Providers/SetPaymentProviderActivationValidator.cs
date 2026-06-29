using FluentValidation;

namespace TMG.WebAPI.Features.Payments.Providers;

public sealed class SetPaymentProviderActivationValidator : AbstractValidator<SetPaymentProviderActivationRequest>
{
    public SetPaymentProviderActivationValidator()
    {
    }
}
