using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TMG.Domain.Common.Authentication;
using TMG.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;

namespace TMG.Infrastructure.UnitTests;

public sealed class WhenAuthorizingActiveSessionWithRevokedToken_Should
{
    [Fact]
    public async Task Fail()
    {
        var tokenId = Guid.CreateVersion7().ToString("N");
        var revocationService = Substitute.For<IAccessTokenRevocationService>();
        revocationService.IsRevokedAsync(tokenId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = new ActiveSessionAuthorizationHandler(revocationService);
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, tokenId)
            ],
            authenticationType: "Bearer"));
        var context = new AuthorizationHandlerContext(
        [
            new ActiveSessionRequirement()
        ],
            user,
            resource: null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }
}

