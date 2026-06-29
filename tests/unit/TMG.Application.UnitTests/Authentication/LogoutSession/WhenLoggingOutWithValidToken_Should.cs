using TMG.Domain.Common.Auditing;
using TMG.Application.Authentication.Features.LogoutSession;
using TMG.Application.UnitTests.Authentication;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests;

public sealed class WhenLoggingOutWithValidToken_Should
{
    [Fact]
    public async Task RevokeAccessToken()
    {
        var context = new AuthenticationFlowTestContext();
        var tokenId = Guid.CreateVersion7().ToString("N");
        var stakeholderId = Guid.CreateVersion7();
        var expiresAtUtc = context.Clock.GetUtcNow().AddMinutes(5);

        var result = await context.CreateLogoutSessionHandler().HandleAsync(
            new LogoutSessionCommand(tokenId, expiresAtUtc, stakeholderId, new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(LogoutSessionStatus.Success);
        await context.AccessTokenRevocationService.Received(1).RevokeAsync(tokenId, expiresAtUtc, Arg.Any<CancellationToken>());
    }
}

