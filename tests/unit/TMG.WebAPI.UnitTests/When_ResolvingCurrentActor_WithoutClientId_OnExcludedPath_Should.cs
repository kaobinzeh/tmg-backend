using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests;

public sealed class When_ResolvingCurrentActor_WithoutClientId_OnExcludedPath_Should
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/readiness")]
    [InlineData("/metrics")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/")]
    [InlineData("/api/v1/payments/webhooks/credo")]
    [InlineData("/api/v1/payments/webhooks/safehaven")]
    [InlineData("/api/v1/email-notifications/webhooks/mailtrap")]
    public async Task AllowRequest(string path)
    {
        var currentActorAccessor = Substitute.For<ICurrentActorAccessor>();
        var stakeholderRepository = Substitute.For<IStakeholderReadModelRepository>();
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = path;

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository, problemDetailsService);

        nextWasCalled.ShouldBeTrue();
        await problemDetailsService.DidNotReceive().WriteAsync(Arg.Any<ProblemDetailsContext>());
    }
}
