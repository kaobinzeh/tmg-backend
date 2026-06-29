using System.Security.Claims;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Authentication;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace TMG.WebAPI.UnitTests;

public sealed class WhenResolvingCurrentActorForAuthenticatedStakeholder_Should
{
    [Fact]
    public async Task UseStakeholderId()
    {
        var stakeholderId = Guid.CreateVersion7();
        var currentActorAccessor = new FakeCurrentActorAccessor();
        var stakeholderRepository = new FakeStakeholderReadModelRepository();
        var problemDetailsService = Substitute.For<IProblemDetailsService>();
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = Guid.CreateVersion7().ToString("N");
        httpContext.Request.Headers["X-Client-Id"] = Guid.CreateVersion7().ToString();
        httpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(CustomClaimTypes.StakeholderId, stakeholderId.ToString()),
                    new Claim(ClaimTypes.NameIdentifier, Guid.CreateVersion7().ToString())
                ],
                "Bearer"));

        var nextWasCalled = false;
        var sut = new CurrentActorMiddleware(_ =>
        {
            nextWasCalled = true;
            return Task.CompletedTask;
        });

        await sut.InvokeAsync(httpContext, currentActorAccessor, stakeholderRepository, problemDetailsService);

        currentActorAccessor.ActorId.ShouldBe(stakeholderId.ToString());
        nextWasCalled.ShouldBeTrue();
        stakeholderRepository.Calls.ShouldBe(0);
    }

    private sealed class FakeCurrentActorAccessor : ICurrentActorAccessor
    {
        public string ActorId { get; private set; } = string.Empty;
        public Guid? ClientId { get; private set; }
        public string CorrelationId { get; private set; } = string.Empty;
        public string FlowId { get; private set; } = string.Empty;

        public void Set(string actorId, Guid? clientId, string correlationId, string flowId)
        {
            ActorId = actorId;
            ClientId = clientId;
            CorrelationId = correlationId;
            FlowId = flowId;
        }
    }

    private sealed class FakeStakeholderReadModelRepository : IStakeholderReadModelRepository
    {
        public int Calls { get; private set; }

        public Task<StakeholderReadModel?> GetByAppUserIdAsync(Guid appUserId, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<StakeholderReadModel?>(null);
        }

        public Task<StakeholderReadModel?> GetByStakeholderIdAsync(Guid stakeholderId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StakeholderReadModel?>(null);
    }
}
