namespace TMG.Domain.Common.Notifications;

/// <summary>
/// Renders a rent-payment receipt and archives it in the per-unit document vault, returning the created
/// document's id. Shared by the manager-recorded (offline) and in-app rent payment paths so both produce an
/// identical receipt. The document is added to the unit of work but not saved — the caller commits.
/// </summary>
public interface IRentReceiptArchiver
{
    Task<Guid> ArchiveAsync(
        Guid clientId,
        Guid tenancyId,
        RentReceiptModel model,
        Guid? uploadedByStakeholderId,
        CancellationToken cancellationToken);
}
