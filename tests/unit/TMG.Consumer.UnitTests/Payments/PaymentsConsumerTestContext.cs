using TMG.Consumer.Payments;
using TMG.Contracts.Commands.Payments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Notifications;
using TMG.Domain.Common.Observability;
using TMG.Domain.Payments.Entities;
using TMG.Domain.Properties.Entities;
using TMG.Domain.Stakeholders.ReadModels;
using TMG.Domain.Tenancies.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;

namespace TMG.Consumer.UnitTests.Payments;

internal sealed class PaymentsConsumerTestContext
{
    public ICustomTelemetryContext CustomTelemetryContext { get; } = Substitute.For<ICustomTelemetryContext>();
    public ICurrentActorAccessor CurrentActorAccessor { get; } = Substitute.For<ICurrentActorAccessor>();
    public IMessageContext MessageContext { get; } = Substitute.For<IMessageContext>();
    public ICommandSender CommandSender { get; } = Substitute.For<ICommandSender>();
    public IRepository<Currency> CurrencyRepository { get; } = Substitute.For<IRepository<Currency>>();
    public IRepository<Wallet> WalletRepository { get; } = Substitute.For<IRepository<Wallet>>();
    public IRepository<WalletTransaction> WalletTransactionRepository { get; } = Substitute.For<IRepository<WalletTransaction>>();
    public IRepository<SubscriptionActivation> SubscriptionActivationRepository { get; } = Substitute.For<IRepository<SubscriptionActivation>>();
    public IRepository<Tenancy> TenancyRepository { get; } = Substitute.For<IRepository<Tenancy>>();
    public IRepository<Unit> UnitRepository { get; } = Substitute.For<IRepository<Unit>>();
    public IRepository<Property> PropertyRepository { get; } = Substitute.For<IRepository<Property>>();
    public IRepository<RentPayment> RentPaymentRepository { get; } = Substitute.For<IRepository<RentPayment>>();
    public IStakeholderReadModelRepository StakeholderReadModelRepository { get; } = Substitute.For<IStakeholderReadModelRepository>();
    public IRentReceiptArchiver ReceiptArchiver { get; } = Substitute.For<IRentReceiptArchiver>();
    public IEventPublisher EventPublisher { get; } = Substitute.For<IEventPublisher>();
    public IUnitOfWork UnitOfWork { get; } = Substitute.For<IUnitOfWork>();
    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 4, 25, 14, 0, 0, TimeSpan.Zero));

    public SuccessfulPaymentConfirmedHandler CreateSuccessfulPaymentConfirmedHandler() =>
        new(CustomTelemetryContext, CurrentActorAccessor, MessageContext, CommandSender, UnitOfWork);

    public CreditWalletHandler CreateCreditWalletHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            CurrencyRepository,
            WalletRepository,
            WalletTransactionRepository,
            UnitOfWork);

    public ActivateSubscriptionHandler CreateActivateSubscriptionHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            SubscriptionActivationRepository,
            UnitOfWork,
            Clock);

    public RecordRentPaymentHandler CreateRecordRentPaymentHandler() =>
        new(
            CustomTelemetryContext,
            CurrentActorAccessor,
            MessageContext,
            TenancyRepository,
            UnitRepository,
            PropertyRepository,
            RentPaymentRepository,
            StakeholderReadModelRepository,
            ReceiptArchiver,
            EventPublisher,
            UnitOfWork,
            Clock);

    public RecordRentPaymentCommand CreateRecordRentPaymentCommand(Guid tenancyId, decimal amount, Guid currencyId, Guid clientId) =>
        new(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", tenancyId, amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            ClientId = clientId,
            FlowId = Guid.CreateVersion7().ToString("N")
        };

    public static Tenancy CreateActiveTenancy(Guid clientId)
    {
        var tenancy = Tenancy.Invite(clientId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "tenant@example.com");
        var cycleStart = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        tenancy.Accept(cycleStart);
        tenancy.Activate(cycleStart, 12);
        return tenancy;
    }

    public CreditWalletCommand CreateCreditWalletCommand(decimal amount, Guid currencyId)
    {
        var command = new CreditWalletCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            ClientId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        return command;
    }

    public ActivateSubscriptionCommand CreateActivateSubscriptionCommand(decimal amount, Guid currencyId)
    {
        var command = new ActivateSubscriptionCommand(Guid.CreateVersion7(), $"pay_{Guid.CreateVersion7():N}", amount, currencyId)
        {
            StakeholderId = Guid.CreateVersion7(),
            ClientId = Guid.CreateVersion7(),
            FlowId = Guid.CreateVersion7().ToString("N")
        };

        return command;
    }

    public void SetCorrelationId() => MessageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));

    public Currency CreateCurrency(Guid id, string currencyCode)
    {
        var currency = Currency.Create(currencyCode, currencyCode, true);
        typeof(Domain.Common.Entities.Entity)
            .GetProperty(nameof(Domain.Common.Entities.Entity.Id))!
            .SetValue(currency, id);
        return currency;
    }

    internal sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

