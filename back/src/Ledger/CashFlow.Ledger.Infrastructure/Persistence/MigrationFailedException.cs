namespace CashFlow.Ledger.Infrastructure.Persistence;

public sealed class MigrationFailedException(string message) : Exception(message);
