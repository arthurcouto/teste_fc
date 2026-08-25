using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using CashFlow.Consolidation.Application;
using CashFlow.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CashFlow.Consolidation.Infrastructure.Messaging;

internal sealed partial class EntryRecordedConsumer(
    IAmazonSQS sqs,
    IServiceScopeFactory scopeFactory,
    ConsolidationInfrastructureOptions options,
    ILogger<EntryRecordedConsumer> logger) : BackgroundService
{

    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Started(logger, options.QueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var received = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = options.QueueUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = options.ReceiveWaitSeconds
                }, stoppingToken);

                foreach (var message in received.Messages ?? [])
                {
                    if (!await HandleAsync(message, stoppingToken))
                    {
                        Log.ConsumptionSuspended(logger, options.VisibilityBackoffSeconds);
                        await Task.Delay(TimeSpan.FromSeconds(options.VisibilityBackoffSeconds), stoppingToken);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log.ReceiveFailed(logger, exception);
                await Task.Delay(TimeSpan.FromSeconds(options.VisibilityBackoffSeconds), stoppingToken);
            }
        }
    }

    private async Task<bool> HandleAsync(Message message, CancellationToken cancellationToken)
    {
        EntryRecorded? integrationEvent;

        try
        {
            integrationEvent = JsonSerializer.Deserialize<EntryRecorded>(message.Body, SerializerOptions);
        }
        catch (JsonException exception)
        {
            Log.Undeserializable(logger, message.MessageId, exception);
            return true;
        }

        if (integrationEvent is null)
        {
            Log.Undeserializable(logger, message.MessageId, null);
            return true;
        }

        using var scope = scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IncorporateEntryHandler>();

        try
        {
            var outcome = await handler.HandleAsync(integrationEvent, cancellationToken);
            Log.Handled(logger, integrationEvent.EntryId, integrationEvent.CorrelationId, outcome);

            await sqs.DeleteMessageAsync(options.QueueUrl, message.ReceiptHandle, cancellationToken);
            return true;
        }
        catch (Exception exception) when (IsUnprocessable(exception))
        {
            Log.PoisonMessage(logger, integrationEvent.EntryId, integrationEvent.CorrelationId, exception.Message);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Log.TransientFailure(logger, integrationEvent.EntryId, integrationEvent.CorrelationId, exception.Message);
            await ReleaseForRetryAsync(message, cancellationToken);
            return false;
        }
    }

    private async Task ReleaseForRetryAsync(Message message, CancellationToken cancellationToken) =>
        await sqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest
        {
            QueueUrl = options.QueueUrl,
            ReceiptHandle = message.ReceiptHandle,
            VisibilityTimeout = options.VisibilityBackoffSeconds
        }, cancellationToken);

    private static bool IsUnprocessable(Exception exception) =>
        exception is UnprocessableEntryException or UnsupportedContractVersionException;

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Consuming from {queueUrl}")]
        public static partial void Started(ILogger logger, string queueUrl);

        [LoggerMessage(Level = LogLevel.Error, Message = "Receiving from the queue failed")]
        public static partial void ReceiveFailed(ILogger logger, Exception exception);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Consumption suspended for {seconds}s while the persistence is unavailable")]
        public static partial void ConsumptionSuspended(ILogger logger, int seconds);

        [LoggerMessage(Level = LogLevel.Error, Message = "Message {messageId} could not be deserialized")]
        public static partial void Undeserializable(ILogger logger, string? messageId, Exception? exception);

        [LoggerMessage(Level = LogLevel.Information, Message = "Entry {entryId} with correlation {correlationId} handled with outcome {outcome}")]
        public static partial void Handled(ILogger logger, Guid entryId, string correlationId, IncorporationOutcome outcome);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Transient failure on entry {entryId} with correlation {correlationId}, releasing for retry: {reason}")]
        public static partial void TransientFailure(ILogger logger, Guid entryId, string correlationId, string reason);

        [LoggerMessage(Level = LogLevel.Error, Message = "Entry {entryId} with correlation {correlationId} is unprocessable and stays on the queue until the redrive policy moves it to the exception queue: {reason}")]
        public static partial void PoisonMessage(ILogger logger, Guid entryId, string correlationId, string reason);
    }
}
