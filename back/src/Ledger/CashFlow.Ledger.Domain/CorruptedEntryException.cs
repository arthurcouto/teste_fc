namespace CashFlow.Ledger.Domain;

public sealed class CorruptedEntryException(string message) : DomainException(message);
