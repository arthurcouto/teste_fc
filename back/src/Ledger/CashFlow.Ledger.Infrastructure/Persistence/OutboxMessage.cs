namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public Guid EntryId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public string? ClaimedBy { get; set; }

    public DateTimeOffset? ClaimedAt { get; set; }

    public int AttemptCount { get; set; }
}
