using CashFlow.Consolidation.Api;
using CashFlow.Ledger.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.IntegrationTests;

public sealed class AuthenticatedApiFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    public const string Issuer = TestTokens.Issuer;
    public const string Audience = TestTokens.Audience;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseSetting("Authentication:Mode", "Required");
        builder.UseSetting("Authentication:Authority", Issuer);
        builder.UseSetting("Authentication:Audience", Audience);
        builder.UseSetting("Ledger:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("Consolidation:ApplyMigrationsOnStartup", "false");

        builder.ConfigureTestServices(services =>
        {
            RemoveBackgroundWorkers(services);
            ReplacePersistencePorts(services);

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = null;
                options.MetadataAddress = null!;
                options.RequireHttpsMetadata = false;
                options.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration();
                options.TokenValidationParameters.IssuerSigningKey = TestTokens.TrustedKey;
                options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
            });
        });
    }

    private static void ReplacePersistencePorts(IServiceCollection services)
    {
        if (typeof(TEntryPoint) == typeof(LedgerApi))
        {
            InMemoryLedger.ReplacePorts(services, new InMemoryEntryRepository());
        }

        if (typeof(TEntryPoint) == typeof(ConsolidationApi))
        {
            InMemoryConsolidation.ReplacePorts(services, new InMemoryDailyBalanceRepository());
        }
    }

    private static void RemoveBackgroundWorkers(IServiceCollection services)
    {
        var workers = services
            .Where(descriptor => descriptor.ImplementationType?.Name is "OutboxPublisherService" or "EntryRecordedConsumer")
            .ToList();

        foreach (var worker in workers)
        {
            services.Remove(worker);
        }
    }
}
