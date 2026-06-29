using FluentValidation;

namespace TMG.WebAPI.Features.Properties;

public sealed class CreatePropertyValidator : AbstractValidator<CreatePropertyRequest>
{
    public CreatePropertyValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Address)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(request => request.Description)
            .MaximumLength(2000);
    }
}
