using TMG.Application.Authentication.Features.CompletePasswordReset;
using TMG.Application.UnitTests.Authentication;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Observability;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenCompletingPasswordResetWithInvalidOtp_Should
{
    [Fact]
    public async Task ReturnInvalidOtp()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var otp = AuthenticationTestData.Otp();

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(user.Id, otp, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await context.CreateCompletePasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateCompletePasswordResetCommand(
                email: user.Email,
                otp: otp),
            CancellationToken.None);

        result.Status.ShouldBe(CompletePasswordResetStatus.InvalidOtp);
        await context.IdentityService.DidNotReceive().ResetPasswordAsync(
            Arg.Any<Domain.Authentication.Entities.AppUser>(),
            Arg.Any<string>());
        context.CustomTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetCompletionFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.InvalidOtp));
    }
}

