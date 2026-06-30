namespace TMG.Application.Tenancies.Features.AcceptTenancyInvitation;

public sealed record AcceptTenancyInvitationResult(
    AcceptTenancyInvitationStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public enum AcceptTenancyInvitationStatus
{
    Success = 1,
    InvalidInvitation = 2,
    TermsNotAccepted = 3,
    InvalidToken = 4,
    ValidationFailed = 5
}
