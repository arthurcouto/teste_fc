namespace CashFlow.Ledger.Domain;

public sealed class InvalidCompetenceDateException(string message) : DomainException(message);
