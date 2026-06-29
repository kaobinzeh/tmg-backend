namespace TMG.Application.Authentication.Features.RequestPasswordReset;

using TMG.Domain.Common.Auditing;

public sealed record RequestPasswordResetCommand(string Email, ActorContext ActorContext);
