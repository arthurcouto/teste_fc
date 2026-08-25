namespace CashFlow.Consolidation.Infrastructure.Persistence;

public sealed class MigrationFailedException(string message) : Exception(message);
