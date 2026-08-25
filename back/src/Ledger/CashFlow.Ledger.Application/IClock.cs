namespace CashFlow.Ledger.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    DateOnly TodayAtMerchant { get; }
}
