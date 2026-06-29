namespace TMG.Domain.Common.Auditing;

public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAtUtc { get; }
    string? DeletedBy { get; }

    void SetDeletedAudit(DateTimeOffset utcNow, string actorId);
}
