using CashFlow.Ledger.Infrastructure.Correlation;

namespace CashFlow.Ledger.Api.Diagnostics;

internal sealed class CorrelationMiddleware(RequestDelegate next)
{
    public const string HeaderName = "x-correlation-id";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        AmbientCorrelationContext.Assign(correlationId);
        context.Items[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await next(context);
    }
}
