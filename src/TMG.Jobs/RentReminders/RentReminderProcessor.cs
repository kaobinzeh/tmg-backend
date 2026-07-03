using TMG.Application.Tenancies.Features.ProcessRentReminders;
using TMG.Jobs.Infrastructure.BackgroundServices;
using Microsoft.Extensions.Options;

namespace TMG.Jobs.RentReminders;

public sealed class RentReminderProcessor(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RentReminderOptions> options,
    BackgroundServiceReadinessState readinessState,
    TimeProvider timeProvider,
    ILogger<RentReminderProcessor> logger) : BackgroundService
{
    public const string ServiceName = nameof(RentReminderProcessor);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        readinessState.MarkReady(ServiceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<RentReminderService>();
                var now = timeProvider.GetUtcNow();

                await service.HandleAsync(
                    now.AddMonths(3),
                    now.AddMonths(1),
                    options.Value.BatchSize,
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rent reminder iteration failed.");
            }

            await Task.Delay(options.Value.PollInterval, stoppingToken);
        }
    }
}
