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

public sealed class When_UpdatingProfile_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnNoContent()
    {
        var currentActor = Substitute.For<ICurrentActor>();
        var stakeholderRepository = Substitute.For<IRepository<Stakeholder>>();
        var customTelemetryContext = Substitute.For<ICustomTelemetryContext>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var stakeholderId = Guid.CreateVersion7();
        var stakeholder = Stakeholder.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Initial", "User");
        var request = new UpdateProfileRequest("Jane", "Doe");

        currentActor.ActorId.Returns(stakeholderId.ToString());
        stakeholderRepository.GetByIdAsync(stakeholderId, Arg.Any<CancellationToken>()).Returns(stakeholder);

        var sut = new ProfilesController(
            new UploadAvatarHandler(stakeholderRepository, Substitute.For<IObjectStorageService>(), customTelemetryContext, unitOfWork),
            new UpdateProfileHandler(stakeholderRepository, customTelemetryContext, unitOfWork),
            currentActor);

        var result = await sut.UpdateProfile(request, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }
}


