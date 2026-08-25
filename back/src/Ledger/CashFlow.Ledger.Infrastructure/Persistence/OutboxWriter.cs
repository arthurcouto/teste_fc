using System.Text.Json;
using CashFlow.Contracts;
using CashFlow.Ledger.Application;

namespace CashFlow.Ledger.Infrastructure.Persistence;

internal sealed class OutboxWriter(LedgerDbContext context, IClock clock) : IOutbox
{
    internal static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(EntryRecorded integrationEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        await context.OutboxMessages.AddAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EntryId = integrationEvent.EntryId,
            EventType = nameof(EntryRecorded),
            Payload = JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            CreatedAt = clock.UtcNow
        }, cancellationToken);
    }
}
