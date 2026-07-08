using System.Net;
using TMG.Domain.Authentication.Entities;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using TMG.Application.Authentication.Features.RefreshSession;
using TMG.WebAPI.Features.Authentication.Sessions;
using TMG.WebAPI.UnitTests.Features.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_RefreshingSession_WithValidRefreshToken_Should
{
    [Fact]
    public async Task RotateTokens()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignInRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignInRequest>>();
        var refreshValidator = Substitute.For<IValidator<RefreshSessionRequest>>();
        var request = new RefreshSessionRequest("refresh-token");
        var user = context.CreateUser();
        user.MarkEmailVerified();
        user.SecurityStamp = "stamp";
        var storedRefreshToken = AuthenticationRefreshToken.Create(user.Id, "HASH", user.SecurityStamp, context.Clock.GetUtcNow().AddDays(30));
        var stakeholder = context.CreateStakeholder(user.Id);

        refreshValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.RefreshTokenService.FindByTokenAsync(request.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns(user.SecurityStamp);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, Arg.Any<string>(), Arg.Any<Guid>())
            .Returns(new AccessToken("new-access-token", context.Clock.GetUtcNow().AddMinutes(15)));
        context.RefreshTokenService.RotateAsync(storedRefreshToken, user, Arg.Any<CancellationToken>())
            .Returns(new RefreshToken("new-refresh-token", context.Clock.GetUtcNow().AddDays(7)));

        var sut = new SessionsController(
            context.CreateSignInHandler(),
            context.CreateGoogleSignInHandler(),
            context.CreateLogoutSessionHandler(),
            context.CreateRefreshSessionHandler(),
            validator,
            googleValidator,
            refreshValidator,
            context.Clock,
            context.CurrentActor)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        sut.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        sut.Request.Headers.UserAgent = "Unit Test";

        var result = await sut.Refresh(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<RefreshSessionResponse>();
        payload.AccessToken.ShouldBe("new-access-token");
        payload.RefreshToken.ShouldBe("new-refresh-token");
    }
}


