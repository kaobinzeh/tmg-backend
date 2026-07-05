using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.GetMyProfile;

public sealed class When_GettingMyProfile_WithValidActor_Should
{
    [Fact]
    public async Task ReturnMappedProfile()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();
        var readModel = new StakeholderReadModel(
            stakeholderId,
            Guid.CreateVersion7(),
            "ada@example.com",
            clientId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "property-manager",
            "Ada",
            "Lovelace",
            "https://example.com/avatar.png",
            true);

        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(readModel);

        var handler = new GetMyProfileHandler(stakeholderReadModelRepository);

        var result = await handler.HandleAsync(
            new GetMyProfileCommand(new ActorContext(stakeholderId, clientId, "corr", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(GetMyProfileStatus.Success);
        result.Profile.ShouldNotBeNull();
        result.Profile.Id.ShouldBe(stakeholderId);
        result.Profile.FirstName.ShouldBe("Ada");
        result.Profile.LastName.ShouldBe("Lovelace");
        result.Profile.Email.ShouldBe("ada@example.com");
        result.Profile.AvatarUrl.ShouldBe("https://example.com/avatar.png");
        result.Profile.StakeholderTypeKey.ShouldBe("property-manager");
        result.Profile.ClientId.ShouldBe(clientId);
    }
}
