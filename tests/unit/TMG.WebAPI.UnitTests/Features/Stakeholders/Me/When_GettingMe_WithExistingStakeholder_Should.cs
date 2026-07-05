using TMG.Application.Stakeholders.Features.GetMyProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.WebAPI.Features.Stakeholders.Me;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Stakeholders.Me;

public sealed class When_GettingMe_WithExistingStakeholder_Should
{
    [Fact]
    public async Task ReturnProfile()
    {
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderId = Guid.CreateVersion7();
        var clientId = Guid.CreateVersion7();

        currentActor.ActorId.Returns(stakeholderId.ToString());
        currentActor.ClientId.Returns(clientId);
        currentActor.CorrelationId.Returns("correlation-id");
        currentActor.FlowId.Returns("flow-id");
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                stakeholderId,
                Guid.CreateVersion7(),
                "ada@example.com",
                clientId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                "manager",
                "Ada",
                "Lovelace",
                "https://example.com/avatar.png",
                true));

        var sut = new MeController(new GetMyProfileHandler(stakeholderReadModelRepository), currentActor);

        var result = await sut.GetMe(CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GetMyProfileResponse>();
        payload.Id.ShouldBe(stakeholderId);
        payload.FirstName.ShouldBe("Ada");
        payload.LastName.ShouldBe("Lovelace");
        payload.Email.ShouldBe("ada@example.com");
        payload.AvatarUrl.ShouldBe("https://example.com/avatar.png");
        payload.StakeholderTypeKey.ShouldBe("manager");
        payload.ClientId.ShouldBe(clientId);
    }
}
