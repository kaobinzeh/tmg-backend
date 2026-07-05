using TMG.Domain.Stakeholders.ReadModels;

namespace TMG.Application.Stakeholders.Features.GetMyProfile;

public sealed class GetMyProfileHandler(IStakeholderReadModelRepository stakeholderReadModelRepository)
{
    public async Task<GetMyProfileResult> HandleAsync(GetMyProfileCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new GetMyProfileResult(GetMyProfileStatus.NotAuthenticated);
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(stakeholderId, cancellationToken);
        if (stakeholder is null)
        {
            return new GetMyProfileResult(GetMyProfileStatus.StakeholderNotFound);
        }

        return new GetMyProfileResult(
            GetMyProfileStatus.Success,
            new GetMyProfileResponse(
                stakeholder.StakeholderId,
                stakeholder.FirstName,
                stakeholder.LastName,
                stakeholder.EmailAddress,
                stakeholder.AvatarUrl,
                stakeholder.StakeholderTypeKey,
                stakeholder.ClientId));
    }
}
