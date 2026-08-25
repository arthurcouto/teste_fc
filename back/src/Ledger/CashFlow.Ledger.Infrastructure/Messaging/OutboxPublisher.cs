using System.Text.Json;
using CashFlow.Contracts;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CashFlow.Ledger.Infrastructure.Messaging;

internal sealed partial class OutboxPublisher(
    LedgerDbContext context,
    IIntegrationEventPublisher publisher,
    IClock clock,
    LedgerInfrastructureOptions options,
    ILogger<OutboxPublisher> logger)
{
    private readonly string _instanceId = InstanceIdentifier.Current;

    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        var claimed = await ClaimAsync(cancellationToken);

        var published = 0;
        foreach (var message in claimed)
        {
            var integrationEvent = JsonSerializer.Deserialize<EntryRecorded>(
                message.Payload, OutboxWriter.SerializerOptions);

            if (integrationEvent is null)
            {
                message.AttemptCount++;

                if (message.AttemptCount >= options.OutboxMaxAttempts)
                {
                    Log.Discarded(logger, message.Id, message.AttemptCount);
                }
                else
                {
                    Log.Undeserializable(logger, message.Id);
                }

                await context.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                await publisher.PublishAsync(
                    message.EventType, message.Payload, integrationEvent.CorrelationId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Log.DeliveryFailed(logger, message.Id, exception);
                message.AttemptCount++;
                message.ClaimedBy = null;
                message.ClaimedAt = null;
                await context.SaveChangesAsync(cancellationToken);
                break;
            }

            message.PublishedAt = clock.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            published++;
        }

        return published;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {messageId} could not be deserialized")]
        public static partial void Undeserializable(ILogger logger, Guid messageId);

        [LoggerMessage(Level = LogLevel.Error,
            Message = "Outbox message {messageId} discarded after {attempts} failed attempts")]
        public static partial void Discarded(ILogger logger, Guid messageId, int attempts);

        [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {messageId} could not be delivered")]
        public static partial void DeliveryFailed(ILogger logger, Guid messageId, Exception exception);

        [LoggerMessage(
            Level = LogLevel.Critical,
            Message = "Outbox message {messageId} is stranded after {attempts} delivery attempts and will no longer be retried. The row is preserved and requires operator action.")]
        public static partial void Stranded(ILogger logger, Guid messageId, int attempts, Exception exception);
    }

    private async Task<List<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken)
    {
        var expiry = clock.UtcNow.AddSeconds(-options.OutboxClaimExpirySeconds);

        var candidates = await context.OutboxMessages
            .Where(m => m.PublishedAt == null
                        && m.AttemptCount < options.OutboxMaxAttempts
                        && (m.ClaimedAt == null || m.ClaimedAt < expiry))
            .OrderBy(m => m.CreatedAt)
            .Take(options.OutboxBatchSize)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var claimedAt = clock.UtcNow;

        await context.OutboxMessages
            .Where(m => candidates.Contains(m.Id)
                        && m.PublishedAt == null
                        && (m.ClaimedAt == null || m.ClaimedAt < expiry))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(m => m.ClaimedBy, _instanceId)
                    .SetProperty(m => m.ClaimedAt, claimedAt),
                cancellationToken);

        return await context.OutboxMessages
            .Where(m => candidates.Contains(m.Id)
                        && m.PublishedAt == null
                        && m.ClaimedBy == _instanceId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
