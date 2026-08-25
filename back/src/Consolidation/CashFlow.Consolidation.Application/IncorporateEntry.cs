using CashFlow.Contracts;

namespace CashFlow.Consolidation.Application;

public enum IncorporationOutcome
{
    Incorporated = 1,
    AlreadyProcessed = 2
}

public sealed class IncorporateEntryHandler(
    IDailyBalanceRepository balances,
    IProcessedEntryLog processedEntries,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public Task<IncorporationOutcome> HandleAsync(EntryRecorded entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.ContractVersion != EntryRecorded.CurrentContractVersion)
        {
            throw new UnsupportedContractVersionException(
                $"Unsupported contract version {entry.ContractVersion}; this consumer accepts {EntryRecorded.CurrentContractVersion}.");
        }

        return unitOfWork.ExecuteAsync(async token =>
        {
            var at = clock.UtcNow;

            var claimed = await processedEntries.TryMarkAsProcessedAsync(
                entry.EntryId, entry.CompetenceDate, at, token);

            if (!claimed)
            {
                return IncorporationOutcome.AlreadyProcessed;
            }

            var current = await balances.FindAsync(entry.CompetenceDate, token)
                          ?? DailyBalance.EmptyOn(entry.CompetenceDate);

            await balances.SaveAsync(current.Incorporate(entry.Type, entry.Amount, at), token);

            return IncorporationOutcome.Incorporated;
        }, cancellationToken);
    }
}
