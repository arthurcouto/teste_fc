namespace CashFlow.Consolidation.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
