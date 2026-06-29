using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.UpdateProfile;

public sealed class When_UpdatingProfile_WithValidActorAndPayload_Should
{
    [Fact]
    public async Task PersistProfileChanges()
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Initial", "User");

        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var sut = new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork);

        var result = await sut.HandleAsync(
            new UpdateProfileCommand("Jane", "Doe", new ActorContext(stakeholderId, Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateProfileStatus.Success);
        stakeholder.FirstName.ShouldBe("Jane");
        stakeholder.LastName.ShouldBe("Doe");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}


