using TMG.Application.Stakeholders.Features.UpdateProfile;
using TMG.Application.Stakeholders.Features.UploadAvatar;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;
using TMG.WebAPI.Features.Stakeholders.Profiles;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace TMG.WebAPI.UnitTests.Features.Stakeholders.Profiles;

public sealed class When_UpdatingProfile_WithMissingNames_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        currentActor.ActorId.Returns(Guid.CreateVersion7().ToString());

        var sut = new ProfilesController(
            new UploadAvatarHandler(
                Substitute.For<IRepository<Stakeholder>>(),
                Substitute.For<IObjectStorageService>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>()),
            new UpdateProfileHandler(
                Substitute.For<IRepository<Stakeholder>>(),
                customTelemetryContext,
                Substitute.For<IUnitOfWork>()),
            currentActor);

        var result = await sut.UpdateProfile(new UpdateProfileRequest(string.Empty, string.Empty), CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
