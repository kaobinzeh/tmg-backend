using TMG.Contracts.Common;
using TMG.Domain.Common.Auditing;
using System.Diagnostics;

namespace TMG.Infrastructure.Persistence;

internal sealed class CurrentActorAccessor : ICurrentActorAccessor
{
    public string ActorId { get; private set; } = ActorDefaults.SystemActorId;
    public Guid? ClientId { get; private set; }
    public string CorrelationId { get; private set; } = Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N");
    public string FlowId { get; private set; } = Guid.CreateVersion7().ToString("N");

    public void Set(string actorId, Guid? clientId, string correlationId, string flowId)
    {
        ActorId = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
        ClientId = clientId;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.CreateVersion7().ToString("N") : correlationId;
        FlowId = flowId ?? string.Empty;
    }
}
