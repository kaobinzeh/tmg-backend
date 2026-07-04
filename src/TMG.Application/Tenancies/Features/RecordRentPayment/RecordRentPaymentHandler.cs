using System.Text;
using TMG.Contracts.Events;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Common.Storage;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;

namespace TMG.Application.Tenancies.Features.RecordRentPayment;

/// <summary>
/// Manager-recorded (offline) rent payment: records the payment against the tenancy's rent cycle, rolls the
/// cycle forward, renders a receipt into the per-unit document vault, and raises <see cref="RentPaymentReceived"/>
/// (the Consumer emails the tenant their receipt).
/// </summary>
public sealed class RecordRentPaymentHandler(
    IRepository<Tenancy> tenancyRepository,
    IRepository<Unit> unitRepository,
    IRepository<Property> propertyRepository,
    IRepository<RentPayment> rentPaymentRepository,
    IRepository<TenancyDocument> tenancyDocumentRepository,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    IRentReceiptRenderer rentReceiptRenderer,
    IObjectStorageService objectStorageService,
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

        var receiptNumber = BuildReceiptNumber(rentPayment.Id);
        var tenant = await stakeholderReadModelRepository.GetByStakeholderIdAsync(tenancy.TenantStakeholderId, cancellationToken);
        var tenantName = tenant is null
            ? tenancy.InvitedEmail
            : $"{tenant.FirstName} {tenant.LastName}".Trim();

        var unitLabel = unit?.Label ?? string.Empty;
        var propertyName = property?.Name ?? string.Empty;

        // Render + archive the receipt in the per-unit vault, then link it to the payment ledger entry.
        var receiptHtml = rentReceiptRenderer.Render(new RentReceiptModel(
            receiptNumber,
            tenantName,
            propertyName,
            unitLabel,
            command.Amount,
            paidAtUtc,
            period.StartUtc,
            period.EndUtc,
            DescribeMethod(command.Method),
            command.Reference));

        var objectKey =
            $"tenants/{clientId}/tenancies/{tenancy.Id}/documents/receipt/{Guid.CreateVersion7():N}.html";
        using var receiptStream = new MemoryStream(Encoding.UTF8.GetBytes(receiptHtml));
        var storageKey = await objectStorageService.UploadPrivateAsync(
            new ObjectStorageUploadRequest(objectKey, receiptStream, "text/html"),
            cancellationToken);

        var document = TenancyDocument.Create(
            clientId,
            tenancy.Id,
            TenancyDocumentType.Receipt,
            storageKey,
            "text/html",
            uploadedByStakeholderId: stakeholderId);
        await tenancyDocumentRepository.AddAsync(document, cancellationToken);

        rentPayment.AttachReceipt(document.Id);
        await rentPaymentRepository.AddAsync(rentPayment, cancellationToken);

        await eventPublisher.PublishAsync(
            new RentPaymentReceived
            {
                TenancyId = tenancy.Id,
                UnitId = tenancy.UnitId,
                PropertyId = tenancy.PropertyId,
                RentPaymentId = rentPayment.Id,
                ReceiptNumber = receiptNumber,
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

        return new RecordRentPaymentResult(RecordRentPaymentStatus.Success, rentPayment.Id, document.Id);
    }

    private static string BuildReceiptNumber(Guid rentPaymentId) =>
        $"RCPT-{rentPaymentId.ToString("N")[..8].ToUpperInvariant()}";

    private static string DescribeMethod(RentPaymentMethod method) => method switch
    {
        RentPaymentMethod.BankTransfer => "Bank transfer",
        RentPaymentMethod.Cash => "Cash",
        RentPaymentMethod.Cheque => "Cheque",
        RentPaymentMethod.Card => "Card",
        _ => "Other"
    };
}
