using System.Net;
using System.Net.Http.Headers;
using CashFlow.Consolidation.Api;
using CashFlow.Ledger.Api;
using Shouldly;

namespace CashFlow.IntegrationTests;

public sealed class LedgerEdgeAuthenticationTests : EdgeAuthenticationContract<LedgerApi>
{
    protected override string BusinessRoute => "/api/v1/entries/?from=2026-01-01&to=2026-01-31";
}

public sealed class ConsolidationEdgeAuthenticationTests : EdgeAuthenticationContract<ConsolidationApi>
{
    protected override string BusinessRoute => "/api/v1/daily-balances/2026-01-01";
}

public abstract class EdgeAuthenticationContract<TEntryPoint> : IDisposable
    where TEntryPoint : class
{
    private readonly AuthenticatedApiFactory<TEntryPoint> factory = new();

    protected abstract string BusinessRoute { get; }

    [Fact]
    public async Task LivenessIsReachableWithoutACredential()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri("/health/live", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BusinessRouteIsRejectedWithoutACredential()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(
            new Uri(BusinessRoute, UriKind.Relative), TestContext.Current.CancellationToken);

        await ShouldBeUnauthenticatedAsync(response);
    }

    [Fact]
    public async Task BusinessRouteIsRejectedWhenTheCredentialExpired()
    {
        using var response = await SendWithTokenAsync(TestTokens.Expired());

        await ShouldBeUnauthenticatedAsync(response);
    }

    [Fact]
    public async Task BusinessRouteIsRejectedWhenTheSignatureIsForeign()
    {
        using var response = await SendWithTokenAsync(TestTokens.ForeignlySigned());

        await ShouldBeUnauthenticatedAsync(response);
    }

    [Fact]
    public async Task BusinessRouteAdmitsAValidCredential()
    {
        using var response = await SendWithTokenAsync(TestTokens.Valid());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task ShouldBeUnauthenticatedAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        body.ShouldContain("unauthenticated");
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(string token)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(BusinessRoute, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
