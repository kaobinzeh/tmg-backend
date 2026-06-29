using TMG.Application.Authentication.Features.SignUpOtp;
using TMG.WebAPI.Features.Authentication.EmailConfirmations;
using TMG.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Authentication.EmailConfirmations;

public sealed class When_VerifyingSignUpOtp_WithAlreadyVerifiedAccount_Should
{
    [Fact]
    public async Task ReturnSuccessMessage()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpOtpRequest>>();
        var request = new SignUpOtpRequest("jane@example.com", "123456");
        var user = context.CreateUser(request.Email);
        user.MarkEmailVerified();

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);

        var sut = new EmailConfirmationsController(context.CreateSignUpOtpHandler(), validator, context.CurrentActor);

        var result = await sut.Handle(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<SignUpOtpResponse>();
        payload.Message.ShouldContain("already verified", Case.Insensitive);
    }
}

