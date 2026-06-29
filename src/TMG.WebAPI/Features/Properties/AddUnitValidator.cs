using FluentValidation;

namespace TMG.WebAPI.Features.Properties;

public sealed class AddUnitValidator : AbstractValidator<AddUnitRequest>
{
    public AddUnitValidator()
    {
        RuleFor(request => request.Label)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(request => request.Description)
            .MaximumLength(2000);

        RuleFor(request => request.NumberOfRooms)
            .GreaterThan(0);

        RuleFor(request => request.RentAmount)
            .GreaterThanOrEqualTo(0);

        RuleFor(request => request.CurrencyId)
            .NotEmpty();
    }
}
