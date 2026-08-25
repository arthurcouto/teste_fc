namespace CashFlow.Ledger.Domain;

public sealed class InvalidDescriptionException(string message) : DomainException(message);
