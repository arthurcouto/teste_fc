using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.Ledger.Infrastructure.Messaging;

internal sealed partial class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    LedgerInfrastructureOptions options,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Started(logger, InstanceIdentifier.Current);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<OutboxPublisher>();
                await publisher.PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log.RoundFailed(logger, exception);
            }

            await Task.Delay(NextDelay(options.OutboxPollSeconds), stoppingToken);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Outbox publisher started as {instance}")]
        public static partial void Started(ILogger logger, string instance);

        [LoggerMessage(Level = LogLevel.Error, Message = "Outbox publishing round failed")]
        public static partial void RoundFailed(ILogger logger, Exception exception);
    }

    private static TimeSpan NextDelay(int baseSeconds) =>
        TimeSpan.FromMilliseconds(baseSeconds * 1000 + Random.Shared.Next(0, baseSeconds * 250));
}
