namespace CashFlow.Ledger.Domain;

public sealed class InvalidEntryTypeException(string message) : DomainException(message);
