using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.SignUp;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Authentication.Registrations;
using TMG.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_HandlingRegistration_WithValidRequest_Should
{
    [Fact]
    public async Task AcceptRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignUpRequest>>();
        var request = new SignUpRequest(
            "jane@example.com",
            "P@ssw0rd123!",
            "P@ssw0rd123!",
            Guid.CreateVersion7(),
            "Jane",
            "Doe");

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), request.Password).Returns(IdentityResult.Success);
        var stakeholderType = context.CreateStakeholderType();
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var sut = new RegistrationsController(
            context.CreateSignUpHandler(),
            context.CreateGoogleSignUpHandler(),
            validator,
            googleValidator,
            context.CurrentActor);

        var result = await sut.Handle(request, CancellationToken.None);

        var accepted = result.Result.ShouldBeOfType<AcceptedResult>();
        var response = accepted.Value.ShouldBeOfType<SignUpResponse>();
        response.Email.ShouldBe(request.Email);
    }
}
