namespace TMG.Domain.Common.Auditing;

public interface ICurrentActor
{
    string ActorId { get; }
    Guid? ClientId { get; }
    string CorrelationId { get; }
    string FlowId { get; }
}
