using FluentValidation;
using TMG.Application.Authentication.Constants;

namespace TMG.WebAPI.Features.Authentication.Registrations;

public sealed class GoogleSignUpValidator : AbstractValidator<GoogleSignUpRequest>
{
    public GoogleSignUpValidator()
    {
        RuleFor(request => request.IdToken)
            .NotEmpty();

        RuleFor(request => request.CountryId)
            .NotEmpty();

        RuleFor(request => request.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.StakeholderTypeKey)
            .NotEmpty()
            .Must(key => StakeholderDefaults.Types.ValidKeys.Contains(key))
            .WithMessage($"StakeholderTypeKey must be one of: {string.Join(", ", StakeholderDefaults.Types.ValidKeys)}.");
    }
}
