namespace TMG.Domain.Common.Auditing;

public interface ICurrentActorAccessor : ICurrentActor
{
    void Set(string actorId, Guid? clientId, string correlationId, string flowId);
}
