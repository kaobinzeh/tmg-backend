using TMG.Contracts.Events;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.RecordRentPayment;

/// <summary>
/// Manager-recorded (offline) rent payment: records the payment against the tenancy's rent cycle, rolls the
/// cycle forward, archives a receipt in the per-unit document vault, and raises <see cref="RentPaymentReceived"/>
/// (the Consumer emails the tenant their receipt).
/// </summary>
public sealed class RecordRentPaymentHandler(
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Property> propertyRepository,
    IRepository<RentPayment> rentPaymentRepository,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IRentReceiptArchiver rentReceiptArchiver,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<RecordRentPaymentResult> HandleAsync(RecordRentPaymentCommand command, CancellationToken cancellationToken)
    {
        if (command.ActorContext.ClientId is not { } clientId ||
            command.ActorContext.StakeholderId is not { } stakeholderId)
        {
            return new RecordRentPaymentResult(RecordRentPaymentStatus.NotAuthenticated);
        }

        if (command.Amount <= 0)
        {
            return new RecordRentPaymentResult(RecordRentPaymentStatus.InvalidAmount);
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(command.TenancyId, clientId, asTracking: true),
            cancellationToken);
        if (tenancy is null)
        {
            return new RecordRentPaymentResult(RecordRentPaymentStatus.TenancyNotFound);
        }

        if (tenancy.Status != TenancyStatus.Active)
        {
            return new RecordRentPaymentResult(RecordRentPaymentStatus.NotActive);
        }

        var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken);
        var property = await propertyRepository.GetByIdAsync(tenancy.PropertyId, cancellationToken);

        var paidAtUtc = command.PaidAtUtc ?? timeProvider.GetUtcNow();
        var period = tenancy.RecordRentPayment(paidAtUtc);

        var rentPayment = RentPayment.Record(
            clientId,
            tenancy.Id,
            tenancy.UnitId,
            tenancy.PropertyId,
            command.Amount,
            unit?.CurrencyId ?? Guid.Empty,
            paidAtUtc,
            period,
            command.Method,
            command.Reference,
            recordedByStakeholderId: stakeholderId);

        var tenant = await stakeholderReadModelRepository.GetByStakeholderIdAsync(tenancy.TenantStakeholderId, cancellationToken);
        var tenantName = tenant is null
            ? tenancy.InvitedEmail
            : $"{tenant.FirstName} {tenant.LastName}".Trim();

        var unitLabel = unit?.Label ?? string.Empty;
        var propertyName = property?.Name ?? string.Empty;

        var receiptDocumentId = await rentReceiptArchiver.ArchiveAsync(
            clientId,
            tenancy.Id,
            new RentReceiptModel(
                rentPayment.ReceiptNumber,
                tenantName,
                propertyName,
                unitLabel,
                command.Amount,
                paidAtUtc,
                period.StartUtc,
                period.EndUtc,
                command.Method.ToDisplayName(),
                command.Reference),
            uploadedByStakeholderId: stakeholderId,
            cancellationToken);

        rentPayment.AttachReceipt(receiptDocumentId);
        await rentPaymentRepository.AddAsync(rentPayment, cancellationToken);

        await eventPublisher.PublishAsync(
            new RentPaymentReceived
            {
                TenancyId = tenancy.Id,
                UnitId = tenancy.UnitId,
                PropertyId = tenancy.PropertyId,
                RentPaymentId = rentPayment.Id,
                ReceiptNumber = rentPayment.ReceiptNumber,
                Amount = command.Amount,
                CurrencyId = rentPayment.CurrencyId,
                PaidAtUtc = paidAtUtc,
                PeriodEndUtc = period.EndUtc,
                UnitLabel = unitLabel,
                PropertyName = propertyName,
                StakeholderId = tenancy.TenantStakeholderId,
                ClientId = tenancy.ClientId,
                FlowId = command.ActorContext.FlowId,
                OccuredAt = timeProvider.GetUtcNow()
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RecordRentPaymentResult(RecordRentPaymentStatus.Success, rentPayment.Id, receiptDocumentId);
    }
}
