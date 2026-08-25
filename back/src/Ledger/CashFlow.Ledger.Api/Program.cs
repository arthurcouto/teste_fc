using CashFlow.Ledger.Api.Authentication;
using CashFlow.Ledger.Api.Diagnostics;
using CashFlow.Ledger.Api.Endpoints;
using CashFlow.Ledger.Infrastructure;
using CashFlow.Ledger.Infrastructure.Correlation;
using CashFlow.Ledger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLedgerInfrastructure(builder.Configuration);
builder.Services.AddSingleton<AmbientCorrelationContext>();
builder.Services.AddApiAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var application = builder.Build();

if (application.Configuration.GetValue("Ledger:ApplyMigrationsOnStartup", true))
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
application.MapEntryEndpoints();

application.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
application.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

await application.RunAsync();

namespace CashFlow.Ledger.Api
{
    public sealed class LedgerApi;
}
