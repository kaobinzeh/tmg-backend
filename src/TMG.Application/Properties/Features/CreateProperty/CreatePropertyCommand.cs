using TMG.Domain.Common.Auditing;

namespace TMG.Application.Properties.Features.CreateProperty;

public sealed record CreatePropertyCommand(
    string Name,
    string Address,
    string? Description,
    ActorContext ActorContext);
