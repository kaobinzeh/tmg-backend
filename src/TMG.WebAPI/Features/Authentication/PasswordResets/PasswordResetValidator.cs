using FluentValidation;

namespace TMG.WebAPI.Features.Authentication.PasswordResets;

public sealed class PasswordResetValidator : AbstractValidator<PasswordResetRequest>
{
    public PasswordResetValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
