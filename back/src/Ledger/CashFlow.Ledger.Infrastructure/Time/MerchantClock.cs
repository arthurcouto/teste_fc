using CashFlow.Ledger.Application;

namespace CashFlow.Ledger.Infrastructure.Time;

internal sealed class MerchantClock : IClock
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _merchantTimeZone;

    public MerchantClock(TimeProvider timeProvider, LedgerInfrastructureOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _timeProvider = timeProvider;
        _merchantTimeZone = TimeZoneInfo.FindSystemTimeZoneById(options.MerchantTimeZone);
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public DateOnly TodayAtMerchant =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _merchantTimeZone).DateTime);
}
