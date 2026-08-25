using CashFlow.Ledger.Application;

namespace CashFlow.Ledger.Infrastructure.Correlation;

public sealed class AmbientCorrelationContext : ICorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId => Current.Value ?? "unassigned";

    public static void Assign(string correlationId) => Current.Value = correlationId;
}
