using FluentValidation;

namespace TMG.WebAPI.Features.Authentication.Sessions;

public sealed class GoogleSignInValidator : AbstractValidator<GoogleSignInRequest>
{
    public GoogleSignInValidator()
    {
        RuleFor(request => request.IdToken)
            .NotEmpty();
    }
}
