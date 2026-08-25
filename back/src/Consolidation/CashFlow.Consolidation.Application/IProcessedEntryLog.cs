namespace CashFlow.Consolidation.Application;

public interface IProcessedEntryLog
{
    Task<bool> TryMarkAsProcessedAsync(
        Guid entryId,
        DateOnly competenceDate,
        DateTimeOffset at,
        CancellationToken cancellationToken);
}
