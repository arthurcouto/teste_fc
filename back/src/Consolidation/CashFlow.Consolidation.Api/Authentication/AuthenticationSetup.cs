using CashFlow.Consolidation.Api.Diagnostics;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CashFlow.Consolidation.Api.Authentication;

internal static class AuthenticationSetup
{
    public const string PolicyName = "business";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        var options = new ApiAuthenticationOptions();
        configuration.GetSection(ApiAuthenticationOptions.SectionName).Bind(options);

        if (options.Mode is AuthenticationMode.Disabled)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Authentication may only be disabled in the Development environment. "
                    + $"The current environment is {environment.EnvironmentName}.");
            }

            services.AddAuthentication();
            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyName, policy => policy.RequireAssertion(_ => true));

            return services;
        }

        if (string.IsNullOrWhiteSpace(options.Authority) || string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException(
                "Authentication is required but the identity provider is not fully configured. "
                + "Set both Authentication:Authority and Authentication:Audience, "
                + "or set Authentication:Mode to Disabled for local runs.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.Authority = options.Authority;
                bearer.Audience = options.Audience;
                bearer.RequireHttpsMetadata = options.RequireHttpsMetadata;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = options.Authority,
                    ValidateAudience = true,
                    ValidAudience = options.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await WriteProblemAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "unauthenticated",
                            "The request is not authenticated",
                            "A valid credential is required to reach this resource.");
                    },
                    OnForbidden = context => WriteProblemAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "forbidden",
                        "The credential does not allow this operation",
                        "The presented credential is valid but not sufficient.")
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyName, policy => policy.RequireAuthenticatedUser());

        return services;
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int status,
        string code,
        string title,
        string detail)
    {
        var correlationId = context.Items[CorrelationMiddleware.HeaderName] as string;

        var problem = new ProblemDetails
        {
            Type = $"https://cashflow/errors/{code}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["correlationId"] = correlationId;

        context.Response.StatusCode = status;
        context.Response.Headers[CorrelationMiddleware.HeaderName] = correlationId;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
