using CashFlow.Consolidation.Api.Authentication;
using CashFlow.Consolidation.Api.Contracts;
using CashFlow.Consolidation.Application;

namespace CashFlow.Consolidation.Api.Endpoints;

internal static class BalanceEndpoints
{
    public static void MapBalanceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/daily-balances").WithTags("DailyBalances")
            .RequireAuthorization(AuthenticationSetup.PolicyName);

        group.MapGet("/{date}", async (
            DateOnly date,
            GetDailyBalanceHandler handler,
            CancellationToken cancellationToken) =>
        {
            var balance = await handler.HandleAsync(date, cancellationToken);

            return Results.Ok(balance.ToResponse());
        })
        .WithName("GetDailyBalance")
        .WithSummary("Reads the consolidated balance of a single date");

        group.MapGet("/", async (
            DateOnly from,
            DateOnly to,
            GetBalanceSeriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var series = await handler.HandleAsync(new BalanceSeriesQuery(from, to), cancellationToken);

            return Results.Ok(series.Select(BalanceMapping.ToResponse));
        })
        .WithName("GetBalanceSeries")
        .WithSummary("Reads the continuous series of daily balances for a period");
    }
}
