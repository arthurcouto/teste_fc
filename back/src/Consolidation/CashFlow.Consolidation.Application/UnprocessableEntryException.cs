namespace CashFlow.Consolidation.Application;

public sealed class UnprocessableEntryException(string message) : Exception(message);
