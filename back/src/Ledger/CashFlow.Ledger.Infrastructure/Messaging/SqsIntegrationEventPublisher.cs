using Amazon.SQS;
using Amazon.SQS.Model;

namespace CashFlow.Ledger.Infrastructure.Messaging;

internal interface IIntegrationEventPublisher
{
    Task PublishAsync(string eventType, string payload, string correlationId, CancellationToken cancellationToken);
}

internal sealed class SqsIntegrationEventPublisher(
    IAmazonSQS sqs,
    LedgerInfrastructureOptions options) : IIntegrationEventPublisher
{
    public Task PublishAsync(
        string eventType,
        string payload,
        string correlationId,
        CancellationToken cancellationToken) =>
        sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = options.QueueUrl,
            MessageBody = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["eventType"] = new() { DataType = "String", StringValue = eventType },
                ["correlationId"] = new() { DataType = "String", StringValue = correlationId }
            }
        }, cancellationToken);
}
