using CashFlow.Contracts;

namespace CashFlow.Ledger.Application;

public interface IOutbox
{
    Task AddAsync(EntryRecorded integrationEvent, CancellationToken cancellationToken);
}
