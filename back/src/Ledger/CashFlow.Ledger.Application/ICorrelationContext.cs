namespace CashFlow.Ledger.Application;

public interface ICorrelationContext
{
    string CorrelationId { get; }
}
