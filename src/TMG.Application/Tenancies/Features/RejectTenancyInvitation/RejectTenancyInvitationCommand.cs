using TMG.Domain.Common.Auditing;

namespace TMG.Application.Tenancies.Features.RejectTenancyInvitation;

public sealed record RejectTenancyInvitationCommand(
    string Email,
    string Token,
    ActorContext ActorContext);
