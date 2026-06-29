using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Stakeholders.Entities;
using NSubstitute;
using Shouldly;

namespace TMG.Application.UnitTests.Stakeholders.UpdateProfile;

public sealed class When_UpdatingProfile_WithBlankNames_Should
{
    [Fact]
    public async Task ReturnValidationFailure()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        var sut = new UpdateProfileHandler(
            Substitute.For<IRepository<Stakeholder>>(),
            customTelemetryContext,
            Substitute.For<IUnitOfWork>());

        var result = await sut.HandleAsync(
            new UpdateProfileCommand(" ", " ", new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7().ToString("N"), Guid.CreateVersion7().ToString("N"))),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateProfileStatus.ValidationFailed);
    }
}
