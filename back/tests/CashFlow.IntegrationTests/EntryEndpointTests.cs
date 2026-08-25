using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CashFlow.Ledger.Api.Contracts;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class EntryEndpointTests : IDisposable
{
    private readonly LedgerApiFactory factory = new();

    [Fact]
    public async Task RecordingAValidEntryYieldsCreatedWithItsLocation()
    {
        using var client = factory.CreateClient();

        using var response = await PostAsync(client, """
            {"type":"credit","amount":150.75,"competenceDate":"2026-03-10","description":"  venda  "}
            """);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<EntryResponse>(
            TestContext.Current.CancellationToken);

        created.ShouldNotBeNull();
        created.Id.ShouldNotBe(Guid.Empty);
        created.Type.ShouldBe("credit");
        created.Amount.ShouldBe(150.75m);
        created.CompetenceDate.ShouldBe(new DateOnly(2026, 3, 10));
        created.Description.ShouldBe("venda");
        created.RecordedAt.ShouldBe(InMemoryLedger.Now);

        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.ToString().ShouldEndWith($"/api/v1/entries/{created.Id}");
    }

    [Theory]
    [InlineData("""{"type":"transfer","amount":10,"competenceDate":"2026-03-10"}""")]
    [InlineData("""{"type":"credit","amount":0,"competenceDate":"2026-03-10"}""")]
    [InlineData("""{"type":"credit","amount":-10,"competenceDate":"2026-03-10"}""")]
    [InlineData("""{"type":"credit","amount":10.123,"competenceDate":"2026-03-10"}""")]
    [InlineData("""{"type":"credit","amount":10,"competenceDate":"2026-03-16"}""")]
    public async Task AnInvalidEntryIsRejectedWithADescribedProblem(string body)
    {
        using var client = factory.CreateClient();

        using var response = await PostAsync(client, body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldDescribeTheProblemAsync(response);
    }

    [Fact]
    public async Task AMalformedBodyIsRejected()
    {
        using var client = factory.CreateClient();

        using var response = await PostAsync(client, """{"type":"credit","amount":""");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldDescribeTheProblemAsync(response);
    }

    [Fact]
    public async Task ARecordedEntryIsReadableByItsIdentifier()
    {
        using var client = factory.CreateClient();

        var recorded = await RecordAsync(client, "credit", 42.10m, new DateOnly(2026, 3, 11));

        using var response = await client.GetAsync(
            new Uri($"/api/v1/entries/{recorded.Id}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var read = await response.Content.ReadFromJsonAsync<EntryResponse>(TestContext.Current.CancellationToken);

        read.ShouldNotBeNull();
        read.Id.ShouldBe(recorded.Id);
        read.Amount.ShouldBe(42.10m);
    }

    [Fact]
    public async Task AnUnknownIdentifierYieldsNotFound()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri($"/api/v1/entries/{Guid.NewGuid()}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListingReportsTheTotalAndHonoursThePageWindow()
    {
        using var client = factory.CreateClient();

        await RecordAsync(client, "credit", 1m, new DateOnly(2026, 3, 1));
        await RecordAsync(client, "debit", 2m, new DateOnly(2026, 3, 2));
        await RecordAsync(client, "credit", 3m, new DateOnly(2026, 3, 3));

        var firstPage = await ListAsync(client, "?from=2026-03-01&to=2026-03-31&offset=0&limit=2");

        firstPage.TotalCount.ShouldBe(3);
        firstPage.Offset.ShouldBe(0);
        firstPage.Limit.ShouldBe(2);
        firstPage.Items.Select(item => item.CompetenceDate)
            .ShouldBe([new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 2)]);

        var secondPage = await ListAsync(client, "?from=2026-03-01&to=2026-03-31&offset=2&limit=2");

        secondPage.TotalCount.ShouldBe(3);
        secondPage.Offset.ShouldBe(2);
        secondPage.Items.Select(item => item.CompetenceDate).ShouldBe([new DateOnly(2026, 3, 3)]);
    }

    [Fact]
    public async Task ListingHonoursThePeriodBounds()
    {
        using var client = factory.CreateClient();

        await RecordAsync(client, "credit", 1m, new DateOnly(2026, 3, 1));
        await RecordAsync(client, "credit", 2m, new DateOnly(2026, 3, 9));

        var page = await ListAsync(client, "?from=2026-03-05&to=2026-03-10");

        page.TotalCount.ShouldBe(1);
        page.Limit.ShouldBe(50);
        page.Items.Single().Amount.ShouldBe(2m);
    }

    [Theory]
    [InlineData("?from=2026-03-31&to=2026-03-01")]
    [InlineData("?from=2026-03-01&to=2026-03-31&offset=-1")]
    [InlineData("?from=2026-03-01&to=2026-03-31&limit=0")]
    [InlineData("?from=2026-03-01&to=2026-03-31&limit=201")]
    public async Task AnInvalidPeriodIsRejectedWithADescribedProblem(string query)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri($"/api/v1/entries/{query}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        await ShouldDescribeTheProblemAsync(response);
    }

    private static async Task<EntryResponse> RecordAsync(
        HttpClient client,
        string type,
        decimal amount,
        DateOnly date)
    {
        var body = JsonSerializer.Serialize(
            new RecordEntryRequest(type, amount, date, null), JsonSerializerOptions.Web);

        using var response = await PostAsync(client, body);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<EntryResponse>(TestContext.Current.CancellationToken))!;
    }

    private static async Task<EntryPageResponse> ListAsync(HttpClient client, string query)
    {
        using var response = await client.GetAsync(
            new Uri($"/api/v1/entries/{query}", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return (await response.Content.ReadFromJsonAsync<EntryPageResponse>(TestContext.Current.CancellationToken))!;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        return await client.PostAsync(
            new Uri("/api/v1/entries/", UriKind.Relative), content, TestContext.Current.CancellationToken);
    }

    private static async Task ShouldDescribeTheProblemAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        using var problem = JsonDocument.Parse(payload);

        problem.RootElement.GetProperty("type").GetString().ShouldStartWith("https://cashflow/errors/");
        problem.RootElement.GetProperty("correlationId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
