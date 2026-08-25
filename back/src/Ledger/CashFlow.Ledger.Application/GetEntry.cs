using CashFlow.Ledger.Domain;

namespace CashFlow.Ledger.Application;

public sealed class GetEntryHandler(IEntryRepository repository)
{
    public Task<Entry?> HandleAsync(Guid id, CancellationToken cancellationToken) =>
        repository.FindAsync(id, cancellationToken);
}
