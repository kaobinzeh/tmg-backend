using TMG.Application.Authentication.Features.GoogleSignIn;
using TMG.Application.UnitTests.Authentication;
using TMG.Contracts.Events;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenSigningInWithRegisteredGoogleIdentity_Should
{
    [Fact]
    public async Task ReturnAccessToken()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();
        var subject = Guid.CreateVersion7().ToString("N");
        const string token = "signed-jwt";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            firstName,
            lastName);

        var context = new AuthenticationFlowTestContext();
        var expectedToken = new AccessToken(token, now.AddHours(1));
        var expectedRefreshToken = new RefreshToken("refresh-token", now.AddDays(30));

        context.GoogleIdentityTokenService.ValidateAsync("google-id-token", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByLoginAsync("Google", subject).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id).Returns(expectedToken);
        context.RefreshTokenService.IssueAsync(user, Arg.Any<CancellationToken>()).Returns(expectedRefreshToken);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateGoogleSignInCommand(
                idToken: "google-id-token",
                ipAddress: ipAddress,
                userAgent: userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(GoogleSignInStatus.Success);
        result.Tokens.ShouldNotBeNull();
        result.Tokens.AccessToken.ShouldBe(expectedToken);
        result.Tokens.RefreshToken.ShouldBe(expectedRefreshToken);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message =>
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
    }
}








