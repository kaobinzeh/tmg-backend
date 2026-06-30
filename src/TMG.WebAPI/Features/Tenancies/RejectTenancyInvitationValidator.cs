using FluentValidation;

namespace TMG.WebAPI.Features.Tenancies;

public sealed class RejectTenancyInvitationValidator : AbstractValidator<RejectTenancyInvitationRequest>
{
    public RejectTenancyInvitationValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Token)
            .NotEmpty();
    }
}
