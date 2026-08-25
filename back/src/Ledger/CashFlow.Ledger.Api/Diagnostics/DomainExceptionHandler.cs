using CashFlow.Ledger.Application;
using CashFlow.Ledger.Domain;
using CashFlow.Ledger.Infrastructure.Correlation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Ledger.Api.Diagnostics;

internal sealed partial class DomainExceptionHandler(
    AmbientCorrelationContext correlation,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
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
        var correlationId = httpContext.Items[CorrelationMiddleware.HeaderName] as string
            ?? correlation.CorrelationId;
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
        InvalidMoneyException => Problem("invalid-amount", "The entry amount is invalid", exception.Message),
        InvalidCompetenceDateException => Problem(
            "invalid-competence-date", "The competence date is invalid", exception.Message),
        InvalidDescriptionException => Problem(
            "invalid-description", "The entry description is invalid", exception.Message),
        InvalidEntryTypeException => Problem("invalid-entry-type", "The entry type is invalid", exception.Message),
        RequestValidationException => Problem("invalid-request", "The request is invalid", exception.Message),
        BadHttpRequestException => Problem(
            "malformed-request", "The request body is invalid", "The request body could not be read as JSON."),
        CorruptedEntryException => Problem(
            "corrupted-entry", "A stored entry is inconsistent", "The stored entry could not be read.",
            StatusCodes.Status500InternalServerError),
        _ => Problem(
            "unexpected", "The request could not be completed", "An unexpected failure occurred.",
            StatusCodes.Status500InternalServerError)
    };

    private static ProblemDetails Problem(
        string code,
        string title,
        string detail,
        int status = StatusCodes.Status400BadRequest) => new()
    {
        Type = $"https://cashflow/errors/{code}",
        Title = title,
        Status = status,
        Detail = detail
    };
}
