using FluentValidation;

namespace TMG.WebAPI.Features.Properties;

public sealed class UpdateUnitRentValidator : AbstractValidator<UpdateUnitRentRequest>
{
    public UpdateUnitRentValidator()
    {
        RuleFor(request => request.RentAmount)
            .GreaterThanOrEqualTo(0);
    }
}
