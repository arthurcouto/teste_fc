using System.Globalization;
using CashFlow.Ledger.Application;
using CashFlow.Ledger.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class MerchantClockTests
{
    [Theory]
    [InlineData("2026-03-10T02:30:00Z", "2026-03-09")]
    [InlineData("2026-03-10T03:00:00Z", "2026-03-10")]
    [InlineData("2026-03-09T23:59:59Z", "2026-03-09")]
    [InlineData("2026-07-15T12:00:00Z", "2026-07-15")]
    public void TheCurrentDateIsResolvedAtTheMerchantTimeZone(string instant, string expected)
    {
        using var provider = BuildProvider("America/Sao_Paulo", Instant(instant));
        using var scope = provider.CreateScope();

        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        clock.TodayAtMerchant.ShouldBe(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void TheCurrentDateFollowsTheConfiguredTimeZone()
    {
        var instant = Instant("2026-03-10T02:30:00Z");

        using var utcProvider = BuildProvider("UTC", instant);
        using var utcScope = utcProvider.CreateScope();

        utcScope.ServiceProvider.GetRequiredService<IClock>().TodayAtMerchant
            .ShouldBe(new DateOnly(2026, 3, 10));

        using var tokyoProvider = BuildProvider("Asia/Tokyo", instant);
        using var tokyoScope = tokyoProvider.CreateScope();

        tokyoScope.ServiceProvider.GetRequiredService<IClock>().TodayAtMerchant
            .ShouldBe(new DateOnly(2026, 3, 10));
    }

    [Fact]
    public void TheInstantIsReadInUniversalTime()
    {
        var instant = Instant("2026-03-10T02:30:00Z");

        using var provider = BuildProvider("America/Sao_Paulo", instant);
        using var scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IClock>().UtcNow.ShouldBe(instant);
    }

    private static DateTimeOffset Instant(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

    private static ServiceProvider BuildProvider(string merchantTimeZone, DateTimeOffset utcNow)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Ledger:MerchantTimeZone"] = merchantTimeZone,
            ["Ledger:QueueUrl"] = "https://sqs.invalid/queue"
        }).Build();

        var services = new ServiceCollection();
        services.AddLedgerInfrastructure(configuration);
        services.RemoveAll<TimeProvider>();
        services.AddSingleton<TimeProvider>(new StoppedTimeProvider(utcNow));

        return services.BuildServiceProvider();
    }

    private sealed class StoppedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
