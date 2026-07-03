using FluentValidation;

namespace TMG.WebAPI.Features.Tenancies;

public sealed class AllocateUnitValidator : AbstractValidator<AllocateUnitRequest>
{
    public AllocateUnitValidator()
    {
        RuleFor(request => request.UnitId)
            .NotEmpty();

        RuleFor(request => request.TenantEmail)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.TenantFirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.TenantLastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.TermMonths!.Value)
            .GreaterThan(0)
            .When(request => request.TermMonths.HasValue);
    }
}
