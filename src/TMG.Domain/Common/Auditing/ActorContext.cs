namespace TMG.Domain.Common.Auditing;

public sealed record ActorContext(Guid? StakeholderId, Guid? ClientId, string CorrelationId, string FlowId)
{
    public static ActorContext FromCurrentActor(ICurrentActor currentActor)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            throw new InvalidOperationException(
                $"Unable to resolve stakeholder id from actor id '{currentActor.ActorId}'.");
        }

        return new ActorContext(stakeholderId, currentActor.ClientId, currentActor.CorrelationId, currentActor.FlowId);
    }

    public static ActorContext FromAnonymousActor(ICurrentActor currentActor) =>
        new(null, currentActor.ClientId, currentActor.CorrelationId, currentActor.FlowId);
}
