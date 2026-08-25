namespace CashFlow.Ledger.Domain;

public sealed class InvalidMoneyException(string message) : DomainException(message);
