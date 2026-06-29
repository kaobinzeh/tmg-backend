using TMG.Contracts.Commands.Payments;
using TMG.Contracts.Events;
using TMG.Contracts.Payments;
using TMG.Domain.Common.Auditing;
using TMG.Domain.Common.Messaging;
using TMG.Domain.Common.Observability;
using TMG.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace TMG.Consumer.Payments;

public sealed class SuccessfulPaymentConfirmedHandler(
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork) : BaseMessageHandler<SuccessfulPaymentConfirmed>(customTelemetryContext, currentActorAccessor, messageContext)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SuccessfulPaymentConfirmed message, CancellationToken cancellationToken)
    {
        switch (message.PaymentIntent)
        {
            case PaymentIntent.WalletTopUp:
                await commandSender.SendAsync(
                    new CreditWalletCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        ClientId = message.ClientId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                CustomTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.CreditWallet,
                    ObservabilityEventProperties.Create(
                        CurrentActorAccessor,
                        message.StakeholderId,
                        additionalProperties: new Dictionary<string, string>
                        {
                            [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                            [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                        }));
                break;

            case PaymentIntent.Subscription:
                await commandSender.SendAsync(
                    new ActivateSubscriptionCommand(
                        message.PaymentTransactionId,
                        message.MerchantReference,
                        message.Amount,
                        message.CurrencyId)
                    {
                        StakeholderId = message.StakeholderId,
                        ClientId = message.ClientId,
                        FlowId = message.FlowId
                    },
                    cancellationToken);
                CustomTelemetryContext.AddCustomEvent(
                    Observability.EventNames.Payments.ActivateSubscription,
                    ObservabilityEventProperties.Create(
                        CurrentActorAccessor,
                        message.StakeholderId,
                        additionalProperties: new Dictionary<string, string>
                        {
                            [Observability.PropertyNames.Payments.PaymentReference] = message.MerchantReference,
                            [Observability.PropertyNames.Payments.CurrencyId] = message.CurrencyId.ToString()
                        }));
                break;

            default:
                throw new CannotProcessMessageNonTransientException(
                    $"Unsupported payment intent '{message.PaymentIntent}'.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
