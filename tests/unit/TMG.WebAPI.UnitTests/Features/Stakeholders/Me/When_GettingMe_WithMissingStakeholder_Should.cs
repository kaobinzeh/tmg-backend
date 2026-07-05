using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Features.Stakeholders.Me;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Stakeholders.Me;

public sealed class When_GettingMe_WithMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderId = Guid.CreateVersion7();

        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.ClientId.Returns(Guid.CreateVersion7());
        currentActor.CorrelationId.Returns("correlation-id");
        currentActor.FlowId.Returns("flow-id");
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);

        var sut = new MeController(new GetMyProfileHandler(stakeholderReadModelRepository), currentActor);

        var result = await sut.GetMe(CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }
}
