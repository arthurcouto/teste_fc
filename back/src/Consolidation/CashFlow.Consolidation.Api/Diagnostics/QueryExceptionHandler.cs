using CashFlow.Consolidation.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Consolidation.Api.Diagnostics;

internal sealed partial class QueryExceptionHandler(ILogger<QueryExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = Describe(exception);

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            Log.Unhandled(logger, exception);
        }

        problem.Instance = httpContext.Request.Path;
        var correlationId = httpContext.Items[CorrelationMiddleware.HeaderName] as string;
        problem.Extensions["correlationId"] = correlationId;
        httpContext.Response.Headers[CorrelationMiddleware.HeaderName] = correlationId;

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled failure")]
        public static partial void Unhandled(ILogger logger, Exception exception);
    }

    private static ProblemDetails Describe(Exception exception) => exception switch
    {
        BalanceQueryException => new ProblemDetails
        {
            Type = "https://cashflow/errors/invalid-period",
            Title = "The requested period is invalid",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message
        },
        BadHttpRequestException => new ProblemDetails
        {
            Type = "https://cashflow/errors/malformed-request",
            Title = "The request is invalid",
            Status = StatusCodes.Status400BadRequest,
            Detail = "The request could not be read."
        },
        _ => new ProblemDetails
        {
            Type = "https://cashflow/errors/unexpected",
            Title = "The request could not be completed",
            Status = StatusCodes.Status500InternalServerError,
            Detail = "An unexpected failure occurred."
        }
    };
}
