namespace TMG.Domain.Common.Notifications;

/// <summary>
/// Renders the tenancy agreement and archives it in the per-unit document vault, returning the created
/// document's id. The document is added to the unit of work but not saved — the caller commits.
/// </summary>
public interface ITenancyAgreementArchiver
{
    Task<Guid> ArchiveAsync(
        Guid clientId,
        Guid tenancyId,
        TenancyAgreementModel model,
        CancellationToken cancellationToken);
}
