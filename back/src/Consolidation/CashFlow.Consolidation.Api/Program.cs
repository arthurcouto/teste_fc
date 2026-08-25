using CashFlow.Consolidation.Api.Authentication;
using CashFlow.Consolidation.Api.Diagnostics;
using CashFlow.Consolidation.Api.Endpoints;
using CashFlow.Consolidation.Infrastructure;
using CashFlow.Consolidation.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddConsolidationInfrastructure(builder.Configuration);
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddExceptionHandler<QueryExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var application = builder.Build();

if (application.Configuration.GetValue("Consolidation:ApplyMigrationsOnStartup", true))
{
    await application.Services.GetRequiredService<DatabaseMigrator>()
        .ApplyAsync(application.Lifetime.ApplicationStopping);
}

application.UseExceptionHandler();
application.UseMiddleware<CorrelationMiddleware>();
application.UseAuthentication();
application.UseAuthorization();

if (application.Environment.IsDevelopment())
{
    application.MapOpenApi();
}
application.MapBalanceEndpoints();

application.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
application.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

await application.RunAsync();

namespace CashFlow.Consolidation.Api
{
    public sealed class ConsolidationApi;
}
