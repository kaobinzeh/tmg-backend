using TMG.Application.Tenancies.Features.RecordRentPayment;
using TMG.Contracts.Events;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.RecordRentPayment;

public sealed class When_RecordingRentPayment_Should
{
    private static readonly DateTimeOffset CycleStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid ReceiptDocumentId = Guid.CreateVersion7();

    private sealed record Harness(
        RecordRentPaymentHandler Handler,
        Tenancy Tenancy,
        Guid ClientId,
        Guid ManagerStakeholderId,
        IRepository<RentPayment> RentPaymentRepository,
        IRentReceiptArchiver ReceiptArchiver,
        IEventPublisher EventPublisher,
        IUnitOfWork UnitOfWork);

    private static Harness Build(TenancyStatus status = TenancyStatus.Active)
    {
        var clientId = Guid.CreateVersion7();
        var managerStakeholderId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var property = Property.Create(clientId, managerStakeholderId, "Lekki Court", "12 Admiralty Way", null);
        var unit = Unit.Create(property.Id, clientId, "Flat 1", null, 2, 1_500_000m, currencyId);

        var tenancy = Tenancy.Invite(clientId, property.Id, unit.Id, Guid.CreateVersion7(), "tenant@example.com");
        if (status is TenancyStatus.Accepted or TenancyStatus.Active)
        {
            tenancy.Accept(CycleStart);
        }

        if (status is TenancyStatus.Active)
        {
            tenancy.Activate(CycleStart, 12);
        }

        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        tenancyRepository
            .FirstOrDefaultAsync(Arg.Any<TenancyByIdForClientSpecification>(), Arg.Any<CancellationToken>())
            .Returns(tenancy);

        var unitRepository = Substitute.For<IRepository<Unit>>();
        unitRepository.GetByIdAsync(unit.Id, Arg.Any<CancellationToken>()).Returns(unit);

        var propertyRepository = Substitute.For<IRepository<Property>>();
        propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        var rentPaymentRepository = Substitute.For<IRepository<RentPayment>>();

        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        stakeholderReadModelRepository
            .GetByStakeholderIdAsync(tenancy.TenantStakeholderId, Arg.Any<CancellationToken>())
            .Returns(new StakeholderReadModel(
                tenancy.TenantStakeholderId, Guid.CreateVersion7(), "tenant@example.com", clientId,
                Guid.CreateVersion7(), Guid.CreateVersion7(), "Tobi", "Ade", null, true));

        var receiptArchiver = Substitute.For<IRentReceiptArchiver>();
        receiptArchiver
            .ArchiveAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<RentReceiptModel>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(ReceiptDocumentId);

        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new RecordRentPaymentHandler(
            tenancyRepository, unitRepository, propertyRepository, rentPaymentRepository,
            stakeholderReadModelRepository, receiptArchiver, eventPublisher, unitOfWork, TimeProvider.System);

        return new Harness(handler, tenancy, clientId, managerStakeholderId,
            rentPaymentRepository, receiptArchiver, eventPublisher, unitOfWork);
    }

    private static RecordRentPaymentCommand Command(Harness harness, decimal amount = 1_500_000m) =>
        new(
            harness.Tenancy.Id,
            amount,
            RentPaymentMethod.BankTransfer,
            new ActorContext(harness.ManagerStakeholderId, harness.ClientId, "corr", "flow"),
            Reference: "TRF-001",
            PaidAtUtc: new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task RecordThePaymentArchiveTheReceiptAndPublishTheEvent()
    {
        var harness = Build();

        var result = await harness.Handler.HandleAsync(Command(harness), CancellationToken.None);

        result.Status.ShouldBe(RecordRentPaymentStatus.Success);
        result.RentPaymentId.ShouldNotBeNull();
        result.ReceiptDocumentId.ShouldBe(ReceiptDocumentId);

        // The first payment settles the current term; the cycle is not advanced.
        harness.Tenancy.CycleEndUtc.ShouldBe(CycleStart.AddMonths(12));

        await harness.RentPaymentRepository.Received(1).AddAsync(
            Arg.Is<RentPayment>(p => p.ReceiptDocumentId == ReceiptDocumentId && p.Amount == 1_500_000m),
            Arg.Any<CancellationToken>());
        await harness.ReceiptArchiver.Received(1).ArchiveAsync(
            harness.ClientId,
            harness.Tenancy.Id,
            Arg.Any<RentReceiptModel>(),
            harness.ManagerStakeholderId,
            Arg.Any<CancellationToken>());
        await harness.EventPublisher.Received(1).PublishAsync(
            Arg.Is<RentPaymentReceived>(e =>
                e.TenancyId == harness.Tenancy.Id && e.StakeholderId == harness.Tenancy.TenantStakeholderId),
            Arg.Any<CancellationToken>());
        await harness.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWhenTheTenancyIsNotActive()
    {
        var harness = Build(TenancyStatus.Accepted);

        var result = await harness.Handler.HandleAsync(Command(harness), CancellationToken.None);

        result.Status.ShouldBe(RecordRentPaymentStatus.NotActive);
        await harness.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectWhenNotAuthenticated()
    {
        var harness = Build();
        var command = Command(harness) with { ActorContext = new ActorContext(null, null, "corr", "flow") };

        var result = await harness.Handler.HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(RecordRentPaymentStatus.NotAuthenticated);
    }
}
