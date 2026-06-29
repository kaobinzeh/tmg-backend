using TMG.Application.Authentication.Features.RefreshSession;
using TMG.Application.UnitTests.Authentication;
using TMG.Domain.Authentication.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenRefreshingSessionAfterPasswordChange_Should
{
    [Fact]
    public async Task InvalidateRefreshToken()
    {
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        user.SecurityStamp = "current-stamp";

        var storedRefreshToken = AuthenticationRefreshToken.Create(user.Id, "HASH", "previous-stamp", now.AddDays(30));

        var context = new AuthenticationFlowTestContext();
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns("current-stamp");

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand("refresh-token"),
            CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.InvalidRefreshToken);
        result.Tokens.ShouldBeNull();
        context.RefreshTokenService.Received(1).Revoke(storedRefreshToken, Arg.Any<DateTimeOffset>());
    }
}







