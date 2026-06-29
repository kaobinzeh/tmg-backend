using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_ForMissingStakeholder_Should
{
    [Fact]
    public async Task ReturnStakeholderNotFound()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var stakeholderId = Guid.CreateVersion7();
        await using var stream = new MemoryStream([1, 2, 3]);

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns((Stakeholder?)null);

        var sut = new UploadAvatarHandler(
            stakeholderRepository,
            Substitute.For<IObjectStorageService>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>());

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.png", "image/png", stream.Length, new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.StakeholderNotFound);
    }
}
