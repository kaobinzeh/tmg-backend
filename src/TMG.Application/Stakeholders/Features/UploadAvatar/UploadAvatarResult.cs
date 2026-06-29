namespace TMG.Application.Stakeholders.Features.UploadAvatar;

public sealed record UploadAvatarResult(
    UploadAvatarStatus Status,
    string? AvatarUrl = null,
    string? Error = null);

public enum UploadAvatarStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3,
    InvalidFile = 4
}
