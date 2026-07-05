using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.GetMyProfile;

public sealed class When_GettingMyProfile_ForMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();

        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((StakeholderReadModel?)null);

        var handler = new GetMyProfileHandler(stakeholderReadModelRepository);

        var result = await handler.HandleAsync(
            new GetMyProfileCommand(new ActorContext(stakeholderId, Guid.CreateVersion7(), "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetMyProfileStatus.StakeholderNotFound);
        result.Profile.ShouldBeNull();
    }
}
