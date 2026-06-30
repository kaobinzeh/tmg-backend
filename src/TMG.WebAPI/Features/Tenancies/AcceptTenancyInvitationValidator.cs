using FluentValidation;

namespace TMG.WebAPI.Features.Tenancies;

public sealed class AcceptTenancyInvitationValidator : AbstractValidator<AcceptTenancyInvitationRequest>
{
    public AcceptTenancyInvitationValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Token)
            .NotEmpty();

        // Password is optional: a returning (already-active) tenant accepts without setting one.
        // When supplied (new pending account), it must meet the strength rules and be confirmed.
        When(request => !string.IsNullOrEmpty(request.Password), () =>
        {
            RuleFor(request => request.Password)
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");

            RuleFor(request => request.ConfirmPassword)
                .Equal(request => request.Password)
                .WithMessage("Password and ConfirmPassword must match.");
        });

        RuleFor(request => request.AcceptedTerms)
            .Equal(true)
            .WithMessage("The terms and conditions must be accepted.");
    }
}
