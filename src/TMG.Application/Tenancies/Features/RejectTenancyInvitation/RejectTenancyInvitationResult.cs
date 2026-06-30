namespace TMG.Application.Tenancies.Features.RejectTenancyInvitation;

public sealed record RejectTenancyInvitationResult(RejectTenancyInvitationStatus Status);

public enum RejectTenancyInvitationStatus
{
    Success = 1,
    InvalidInvitation = 2,
    InvalidToken = 3
}
