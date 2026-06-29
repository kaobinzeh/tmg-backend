using TMG.Application.Authentication.Features.GoogleSignUp;
using TMG.Application.Authentication.Features.SignUp;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.WebAPI.Features.Authentication.Registrations;
using TMG.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Authentication.Registrations;

public sealed class When_HandlingGoogleRegistration_WithDuplicateGoogleAccount_Should
{
    [Fact]
    public async Task ReturnConflict()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignUpRequest>>();
        var request = new GoogleSignUpRequest("google-token", Guid.CreateVersion7(), "Jane", "Doe", "tenant");

        googleValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.GoogleIdentityTokenService.ValidateAsync(request.IdToken, Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(Guid.CreateVersion7().ToString("N"), "jane@example.com", "Jane Doe"));
        context.IdentityService.FindByEmailAsync("jane@example.com").Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(
                Arg.Any<AppUser>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>())
            .Returns(IdentityResult.Failed(new IdentityError { Code = nameof(IdentityErrorDescriber.LoginAlreadyAssociated) }));

        var sut = new RegistrationsController(
            context.CreateSignUpHandler(),
            context.CreateGoogleSignUpHandler(),
            validator,
            googleValidator,
            context.CurrentActor);

        var result = await sut.HandleGoogle(request, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
    }
}
