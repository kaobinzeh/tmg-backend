namespace TMG.Application.Stakeholders.Features.UploadAvatar;

using TMG.Domain.Common.Auditing;

public sealed record UploadAvatarCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long ContentLength,
    ActorContext ActorContext);
