using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_WithValidImageFile_Should
{
    [Fact]
    public async Task PersistAvatarUrl()
    {
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var objectStorageService = Substitute.For<IObjectStorageService>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Jane", "Doe");
        await using var stream = new MemoryStream([1]);

        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        objectStorageService.UploadPublicAsync(Arg.Any<ObjectStorageUploadRequest>(), Arg.Any<CancellationToken>())
            .Returns("https://example.com/avatar.png");

        var sut = new UploadAvatarHandler(
            stakeholderRepository,
            objectStorageService,
            customTelemetryContext,
            unitOfWork);

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.png", "image/png", stream.Length, new ActorContext(stakeholderId, Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.Success);
        stakeholder.AvatarUrl.ShouldBe("https://example.com/avatar.png");
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}


