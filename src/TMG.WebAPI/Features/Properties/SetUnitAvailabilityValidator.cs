using FluentValidation;

namespace TMG.WebAPI.Features.Properties;

public sealed class SetUnitAvailabilityValidator : AbstractValidator<SetUnitAvailabilityRequest>
{
    public SetUnitAvailabilityValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum();
    }
}
