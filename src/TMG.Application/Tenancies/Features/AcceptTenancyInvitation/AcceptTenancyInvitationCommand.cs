using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.AcceptTenancyInvitation;

public sealed record AcceptTenancyInvitationCommand(
    string Email,
    string Token,
    string? Password,
    string? ConfirmPassword,
    bool AcceptedTerms,
    ActorContext ActorContext);
