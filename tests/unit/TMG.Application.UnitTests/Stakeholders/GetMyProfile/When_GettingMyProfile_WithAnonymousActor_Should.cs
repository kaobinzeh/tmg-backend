using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.GetMyProfile;

public sealed class When_GettingMyProfile_WithAnonymousActor_Should
{
    [Fact]
    public async Task ReturnNotAuthenticated()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var handler = new GetMyProfileHandler(stakeholderReadModelRepository);

        var result = await handler.HandleAsync(
            new GetMyProfileCommand(new ActorContext(null, Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetMyProfileStatus.NotAuthenticated);
        result.Profile.ShouldBeNull();
        await stakeholderReadModelRepository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderIdAsync(default, default);
    }
}
