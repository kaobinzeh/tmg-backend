using TMG.Contracts.Commands;

namespace TMG.Domain.Common.Messaging;

public interface ICommandSender
{
    Task SendAsync<TCommand>(TCommand message, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;

    Task ScheduleSendAsync<TCommand>(TCommand message, DateTimeOffset deliverAtUtc, CancellationToken cancellationToken = default)
        where TCommand : BaseCommand;
}
