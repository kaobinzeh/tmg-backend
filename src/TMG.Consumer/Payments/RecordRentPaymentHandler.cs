using TMG.Contracts.Commands.Payments;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Payments;

/// <summary>
/// Settles a confirmed in-app rent payment: records the ledger entry (linked to the gateway transaction),
/// advances the tenancy's rent cycle, archives a receipt in the vault, and raises <see cref="RentPaymentReceived"/>
/// so the tenant is emailed their receipt. Idempotent per payment transaction.
/// </summary>
public sealed class RecordRentPaymentHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Property> propertyRepository,
    IRepository<RentPayment> rentPaymentRepository,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IRentReceiptArchiver rentReceiptArchiver,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : BaseMessageHandler<RecordRentPaymentCommand>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(RecordRentPaymentCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("RecordRentPaymentCommand must contain a valid stakeholder id.");
        }

        // Idempotency: the reconciliation → confirmation pipeline can deliver more than once.
        var existing = await rentPaymentRepository.FirstOrDefaultAsync(
            new RentPaymentByPaymentTransactionSpecification(message.PaymentTransactionId),
            cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var tenancy = await tenancyRepository.FirstOrDefaultAsync(
            new TenancyByIdForClientSpecification(message.TenancyId, message.ClientId, asTracking: true),
            cancellationToken);
        if (tenancy is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to record rent payment because tenancy '{message.TenancyId}' was not found.");
        }

        if (tenancy.Status != TenancyStatus.Active)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Tenancy '{message.TenancyId}' does not have an active rent cycle (status: {tenancy.Status}).");
        }

        var unit = await unitRepository.GetByIdAsync(tenancy.UnitId, cancellationToken);
        var property = await propertyRepository.GetByIdAsync(tenancy.PropertyId, cancellationToken);

        var paidAtUtc = timeProvider.GetUtcNow();
        var period = tenancy.RecordRentPayment(paidAtUtc);

        var rentPayment = RentPayment.Record(
            message.ClientId,
            tenancy.Id,
            tenancy.UnitId,
            tenancy.PropertyId,
            message.Amount,
            unit?.CurrencyId ?? message.CurrencyId,
            paidAtUtc,
            period,
            RentPaymentMethod.Online,
            message.MerchantReference,
            recordedByStakeholderId: null,
            paymentTransactionId: message.PaymentTransactionId);

        var tenant = await stakeholderReadModelRepository.GetByStakeholderIdAsync(tenancy.TenantStakeholderId, cancellationToken);
        var tenantName = tenant is null
            ? tenancy.InvitedEmail
            : $"{tenant.FirstName} {tenant.LastName}".Trim();

        var unitLabel = unit?.Label ?? string.Empty;
        var propertyName = property?.Name ?? string.Empty;

        var receiptDocumentId = await rentReceiptArchiver.ArchiveAsync(
            message.ClientId,
            tenancy.Id,
            new RentReceiptModel(
                rentPayment.ReceiptNumber,
                tenantName,
                propertyName,
                unitLabel,
                message.Amount,
                paidAtUtc,
                period.StartUtc,
                period.EndUtc,
                RentPaymentMethod.Online.ToDisplayName(),
                message.MerchantReference),
            uploadedByStakeholderId: null,
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
                Amount = message.Amount,
                CurrencyId = rentPayment.CurrencyId,
                PaidAtUtc = paidAtUtc,
                PeriodEndUtc = period.EndUtc,
                UnitLabel = unitLabel,
                PropertyName = propertyName,
                StakeholderId = tenancy.TenantStakeholderId,
                ClientId = tenancy.ClientId,
                FlowId = message.FlowId,
                OccuredAt = paidAtUtc
            },
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.RentPaymentRecorded,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                message.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                    [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                }));
    }
}
