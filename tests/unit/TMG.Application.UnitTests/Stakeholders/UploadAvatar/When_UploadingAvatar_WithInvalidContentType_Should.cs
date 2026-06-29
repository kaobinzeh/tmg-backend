using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.UploadAvatar;

public sealed class When_UploadingAvatar_WithInvalidContentType_Should
{
    [Fact]
    public async Task ReturnInvalidFile()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        await using var stream = new MemoryStream([1, 2, 3]);
        var sut = new UploadAvatarHandler(
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<IObjectStorageService>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>());

        var result = await sut.HandleAsync(
            new UploadAvatarCommand(stream, "avatar.txt", "text/plain", stream.Length, new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UploadAvatarStatus.InvalidFile);
        customTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.InvalidFile));
    }
}
