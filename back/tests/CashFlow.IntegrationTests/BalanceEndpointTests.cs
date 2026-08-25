using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CashFlow.Consolidation.Api.Contracts;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class BalanceEndpointTests : IDisposable
{
    private readonly ConsolidationApiFactory factory = new();

    [Fact]
    public async Task ADateWithoutMovementYieldsZeroesAndNoUpdateInstant()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/v1/daily-balances/2026-03-10", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var balance = await response.Content.ReadFromJsonAsync<DailyBalanceResponse>(
            TestContext.Current.CancellationToken);

        balance.ShouldNotBeNull();
        balance.CompetenceDate.ShouldBe(new DateOnly(2026, 3, 10));
        balance.TotalCredits.ShouldBe(0m);
        balance.TotalDebits.ShouldBe(0m);
        balance.Balance.ShouldBe(0m);
        balance.EntryCount.ShouldBe(0);
        balance.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task ADateWithMovementYieldsItsConsolidatedTotals()
    {
        await factory.SeedAsync(new DateOnly(2026, 3, 10), 300.50m, 100.25m, 4);

        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/v1/daily-balances/2026-03-10", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var balance = await response.Content.ReadFromJsonAsync<DailyBalanceResponse>(
            TestContext.Current.CancellationToken);

        balance.ShouldNotBeNull();
        balance.TotalCredits.ShouldBe(300.50m);
        balance.TotalDebits.ShouldBe(100.25m);
        balance.Balance.ShouldBe(200.25m);
        balance.EntryCount.ShouldBe(4);
        balance.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task TheSeriesCoversEveryDayOfThePeriodWithoutGaps()
    {
        await factory.SeedAsync(new DateOnly(2026, 3, 3), 50m, 0m, 1);

        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/v1/daily-balances/?from=2026-03-01&to=2026-03-05", UriKind.Relative),
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var series = await response.Content.ReadFromJsonAsync<IReadOnlyList<DailyBalanceResponse>>(
            TestContext.Current.CancellationToken);

        series.ShouldNotBeNull();
        series.Select(day => day.CompetenceDate).ShouldBe(
        [
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 2),
            new DateOnly(2026, 3, 3),
            new DateOnly(2026, 3, 4),
            new DateOnly(2026, 3, 5)
        ]);

        series[2].Balance.ShouldBe(50m);
        series[0].UpdatedAt.ShouldBeNull();
        series[4].EntryCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("?from=2026-03-31&to=2026-03-01")]
    [InlineData("?from=2026-01-01&to=2027-06-01")]
    public async Task AnInvalidPeriodIsRejectedWithADescribedProblem(string query)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri($"/api/v1/daily-balances/{query}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var problem = JsonDocument.Parse(payload);

        problem.RootElement.GetProperty("type").GetString().ShouldStartWith("https://cashflow/errors/");
        problem.RootElement.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AnUnreadableDateIsRejected()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/api/v1/daily-balances/not-a-date", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
