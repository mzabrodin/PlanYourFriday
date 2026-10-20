using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

using Npgsql;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Api.ErrorHandling;

internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, code, detail) = Map(exception);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
        };

        if (code is not null)
        {
            problem.Extensions["code"] = code;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem,
        });
    }

    private static (int Status, string? Code, string Detail) Map(Exception exception)
    {
        return exception switch
        {
            BadHttpRequestException e => (e.StatusCode, null, e.Message),
            UnauthenticatedException e => (StatusCodes.Status401Unauthorized, null, e.Message),
            AccessDeniedException e => (StatusCodes.Status403Forbidden, e.Code, e.Message),
            EntityNotFoundException e => (StatusCodes.Status404NotFound, e.Code, e.Message),
            InvalidStateException e => (StatusCodes.Status409Conflict, e.Code, e.Message),
            AlreadyExistsException e => (StatusCodes.Status409Conflict, e.Code, e.Message),

            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                => (StatusCodes.Status409Conflict, "already_exists", "The resource already exists."),

            BusinessRuleViolationException e => (StatusCodes.Status422UnprocessableEntity, e.Code, e.Message),
            _ => (StatusCodes.Status500InternalServerError, null, "An unexpected error occurred."),
        };
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string path);
}
