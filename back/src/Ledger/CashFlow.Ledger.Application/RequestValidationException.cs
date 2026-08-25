namespace CashFlow.Ledger.Application;

public sealed class RequestValidationException(string message) : Exception(message);
