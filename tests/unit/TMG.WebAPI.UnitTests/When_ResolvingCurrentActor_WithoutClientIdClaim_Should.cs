using System.Security.Claims;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests;

public sealed class When_ResolvingCurrentActor_WithoutClientIdClaim_Should
{
    [Fact]
    public async Task FallBackToStakeholderReadModelClientId()
    {
        var stakeholderId = Guid.CreateVersion7();
        var appUserId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        stakeholderRepository.GetByAppUserIdAsync(appUserId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                appUserId,
                "stakeholder@example.com",
                clientId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "manager",
                "First",
                "Last",
                null,
                true));
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(CustomClaimTypes.StakeholderId, stakeholderId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, appUserId.ToString())
                ],
                "Bearer"));

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository);

        nextWasCalled.ShouldBeTrue();
        currentActorAccessor.Received(1).Set(
            stakeholderId.ToString(),
            clientId,
            Arg.Any<string>(),
            Arg.Any<string>());
    }
}
