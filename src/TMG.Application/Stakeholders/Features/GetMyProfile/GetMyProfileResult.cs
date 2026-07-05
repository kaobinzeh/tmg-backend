namespace TMG.Application.Stakeholders.Features.GetMyProfile;

public sealed record GetMyProfileResult(
    GetMyProfileStatus Status,
    GetMyProfileResponse? Profile = null);

public enum GetMyProfileStatus
{
    Success = 1,
    NotAuthenticated = 2,
    StakeholderNotFound = 3
}
