namespace CashFlow.Consolidation.Application;

public sealed class UnsupportedContractVersionException(string message) : Exception(message);
