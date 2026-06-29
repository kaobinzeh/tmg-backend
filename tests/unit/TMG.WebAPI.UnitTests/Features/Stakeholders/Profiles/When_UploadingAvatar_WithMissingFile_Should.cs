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

public sealed class When_UploadingAvatar_WithMissingFile_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
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

        var result = await sut.UploadAvatar(new UploadAvatarRequest(null!), CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
