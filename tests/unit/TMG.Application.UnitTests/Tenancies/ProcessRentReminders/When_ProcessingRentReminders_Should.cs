using TMG.Application.Tenancies.Features.ProcessRentReminders;
using TMG.Contracts.Commands.Notifications;
using TMG.Contracts.Events;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Persistence;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Tenancies.Entities;
using TMG.Domain.Tenancies.Specifications;
using Shouldly;

namespace TMG.Application.UnitTests.Tenancies.ProcessRentReminders;

public sealed class When_ProcessingRentReminders_Should
{
    private static readonly DateTimeOffset CycleStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RentDue = CycleStart.AddMonths(12); // 2027-01-01

    private static (Tenancy Tenancy, Unit Unit, Property Property) SeedActiveTenancy()
    {
        var clientId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var property = Property.Create(clientId, stakeholderId, "Lekki Court", "12 Admiralty Way", null);
        var unit = Unit.Create(property.Id, clientId, "Flat 1", null, 2, 1_500_000m, Guid.CreateVersion7());

        var tenancy = Tenancy.Invite(clientId, property.Id, unit.Id, stakeholderId, "tenant@example.com");
        tenancy.Accept(CycleStart);
        tenancy.Activate(CycleStart, 12);
        return (tenancy, unit, property);
    }

    private static RentReminderService BuildService(
        Tenancy tenancy, Unit unit, Property property,
        IEventPublisher eventPublisher, IUnitOfWork unitOfWork)
    {
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        var unitRepository = Substitute.For<IRepository<Unit>>();
        var propertyRepository = Substitute.For<IRepository<Property>>();

        tenancyRepository
            .ListAsync(Arg.Any<ActiveTenanciesDueForReminderSpecification>(), Arg.Any<CancellationToken>())
            .Returns([tenancy]);
        unitRepository.GetByIdAsync(unit.Id, Arg.Any<CancellationToken>()).Returns(unit);
        propertyRepository.GetByIdAsync(property.Id, Arg.Any<CancellationToken>()).Returns(property);

        return new RentReminderService(
            tenancyRepository, unitRepository, propertyRepository, eventPublisher, unitOfWork, TimeProvider.System);
    }

    [Fact]
    public async Task PublishTheThreeMonthReminderWhenOnlyThatWindowIsOpen()
    {
        var (tenancy, unit, property) = SeedActiveTenancy();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = BuildService(tenancy, unit, property, eventPublisher, unitOfWork);

        // Due date is inside the 3-month window but outside the 1-month window.
        var result = await service.HandleAsync(RentDue, RentDue.AddMonths(-2), 100, CancellationToken.None);

        result.Processed.ShouldBe(1);
        tenancy.Reminder3MonthsSentAtUtc.ShouldNotBeNull();
        tenancy.Reminder1MonthSentAtUtc.ShouldBeNull();
        await eventPublisher.Received(1).PublishAsync(
            Arg.Is<RentDueReminderTriggered>(e =>
                e.TenancyId == tenancy.Id && e.ReminderType == NotificationType.RentDueReminder3Months),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTheOneMonthReminderAndSupersedeTheThreeMonthOne()
    {
        var (tenancy, unit, property) = SeedActiveTenancy();
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var service = BuildService(tenancy, unit, property, eventPublisher, unitOfWork);

        // Due date is inside both windows and no reminder has been sent yet.
        var result = await service.HandleAsync(RentDue, RentDue, 100, CancellationToken.None);

        result.Processed.ShouldBe(1);
        tenancy.Reminder1MonthSentAtUtc.ShouldNotBeNull();
        tenancy.Reminder3MonthsSentAtUtc.ShouldNotBeNull(); // superseded, so it never fires late
        await eventPublisher.Received(1).PublishAsync(
            Arg.Is<RentDueReminderTriggered>(e => e.ReminderType == NotificationType.RentDueReminder1Month),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DoNothingWhenNoTenanciesAreDue()
    {
        var eventPublisher = Substitute.For<IEventPublisher>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var tenancyRepository = Substitute.For<IRepository<Tenancy>>();
        tenancyRepository
            .ListAsync(Arg.Any<ActiveTenanciesDueForReminderSpecification>(), Arg.Any<CancellationToken>())
            .Returns([]);

        var service = new RentReminderService(
            tenancyRepository,
            Substitute.For<IRepository<Unit>>(),
            Substitute.For<IRepository<Property>>(),
            eventPublisher,
            unitOfWork,
            TimeProvider.System);

        var result = await service.HandleAsync(RentDue, RentDue, 100, CancellationToken.None);

        result.Processed.ShouldBe(0);
        await eventPublisher.DidNotReceive().PublishAsync(Arg.Any<RentDueReminderTriggered>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
