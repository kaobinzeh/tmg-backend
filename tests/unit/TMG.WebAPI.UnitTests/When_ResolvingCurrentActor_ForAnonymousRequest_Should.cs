using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests;

public sealed class When_ResolvingCurrentActor_ForAnonymousRequest_Should
{
    [Fact]
    public async Task AllowRequestWithoutClientId()
    {
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/test";

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository);

        nextWasCalled.ShouldBeTrue();
        currentActorAccessor.Received(1).Set(
            "anonymous",
            null,
            Arg.Any<string>(),
            Arg.Any<string>());
    }
}
