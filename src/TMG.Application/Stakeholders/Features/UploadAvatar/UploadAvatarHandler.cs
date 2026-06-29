using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Stakeholders.Entities;

namespace TMG.Application.Stakeholders.Features.UploadAvatar;

public sealed class UploadAvatarHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IObjectStorageService objectStorageService,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    private const long MaxAvatarFileSizeBytes = 2 * 1024 * 1024;

    public async Task<UploadAvatarResult> HandleAsync(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.NotAuthenticated);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.AvatarUploadFailed,
                ObservabilityEventProperties.Create(command.ActorContext, failureReason: ObservabilityFailureReasons.NotAuthenticated));
            return new UploadAvatarResult(UploadAvatarStatus.NotAuthenticated);
        }

        if (command.ContentLength <= 0 ||
            command.ContentLength > MaxAvatarFileSizeBytes ||
            string.IsNullOrWhiteSpace(command.ContentType) ||
            !command.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.InvalidFile);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.AvatarUploadFailed,
                ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, ObservabilityFailureReasons.InvalidFile));
            return new UploadAvatarResult(
                UploadAvatarStatus.InvalidFile,
                Error: "Avatar must be an image file with size up to 2 MB.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.StakeholderNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.AvatarUploadFailed,
                ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound));
            return new UploadAvatarResult(UploadAvatarStatus.StakeholderNotFound);
        }

        var fileExtension = Path.GetExtension(command.FileName);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".bin";
        }

        var objectKey =
            $"tenants/{stakeholder.ClientId}/stakeholders/{stakeholder.Id}/avatar/{Guid.CreateVersion7():N}{fileExtension.ToLowerInvariant()}";
        var avatarUrl = await objectStorageService.UploadPublicAsync(
            new ObjectStorageUploadRequest(objectKey, command.Content, command.ContentType),
            cancellationToken);

        stakeholder.SetAvatarUrl(avatarUrl);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.AvatarUploadCompleted,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId));

        return new UploadAvatarResult(UploadAvatarStatus.Success, avatarUrl);
    }
}
