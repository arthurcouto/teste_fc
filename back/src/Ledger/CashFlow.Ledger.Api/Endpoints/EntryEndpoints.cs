using CashFlow.Ledger.Api.Authentication;
using CashFlow.Ledger.Api.Contracts;
using CashFlow.Ledger.Application;

namespace CashFlow.Ledger.Api.Endpoints;

internal static class EntryEndpoints
{
    public static void MapEntryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/entries").WithTags("Entries")
            .RequireAuthorization(AuthenticationSetup.PolicyName);

        group.MapPost("/", async (
            RecordEntryRequest request,
            RecordEntryHandler handler,
            CancellationToken cancellationToken) =>
        {
            ArgumentNullException.ThrowIfNull(request);

            var entry = await handler.HandleAsync(
                new RecordEntryCommand(
                    EntryMapping.ToEntryType(request.Type),
                    request.Amount,
                    request.CompetenceDate,
                    request.Description),
                cancellationToken);

            return Results.Created($"/api/v1/entries/{entry.Id}", entry.ToResponse());
        })
        .WithName("RecordEntry")
        .WithSummary("Records a credit or debit entry");

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetEntryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var entry = await handler.HandleAsync(id, cancellationToken);

            return entry is null ? Results.NotFound() : Results.Ok(entry.ToResponse());
        })
        .WithName("GetEntry")
        .WithSummary("Reads a single entry by its identifier");

        group.MapGet("/", async (
            DateOnly from,
            DateOnly to,
            int? offset,
            int? limit,
            ListEntriesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var period = EntryPeriod.Of(from, to, offset, limit);
            var page = await handler.HandleAsync(new ListEntriesQuery(from, to, offset, limit), cancellationToken);

            return Results.Ok(page.ToResponse(period));
        })
        .WithName("ListEntries")
        .WithSummary("Lists entries within a competence period");
    }
}
