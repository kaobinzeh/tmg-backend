using FluentValidation;

namespace TMG.WebAPI.Features.Authentication.EmailConfirmations;

public sealed class ResendSignUpOtpValidator : AbstractValidator<ResendSignUpOtpRequest>
{
    public ResendSignUpOtpValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
