using TMG.Application.Authentication.Features.RefreshSession;
using TMG.Application.UnitTests.Authentication;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenRefreshingSessionWithLockedAccount_Should
{
    [Fact]
    public async Task ReturnAccountLocked()
    {
        var email = AuthenticationTestData.Email();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var securityStamp = Guid.CreateVersion7().ToString("N");
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        user.SecurityStamp = securityStamp;

        var storedRefreshToken = AuthenticationRefreshToken.Create(
            user.Id,
            "HASH",
            securityStamp,
            now.AddDays(30));

        var context = new AuthenticationFlowTestContext();
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns(securityStamp);
        context.IdentityService.IsLockedOutAsync(user).Returns(true);
        context.IdentityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand("refresh-token"),
            CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.AccountLocked);
        result.Tokens.ShouldBeNull();
        result.LockedUntilUtc.ShouldBe(lockedUntilUtc);
    }
}






