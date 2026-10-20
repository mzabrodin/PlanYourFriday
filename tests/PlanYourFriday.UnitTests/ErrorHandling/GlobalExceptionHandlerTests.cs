using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using PlanYourFriday.Api.ErrorHandling;
using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.UnitTests.ErrorHandling;

public class GlobalExceptionHandlerTests
{
    public static TheoryData<Exception, int, string> Errors => new()
    {
        { new BadHttpRequestException("Malformed body."), 400, "bad_request" },
        { new UnauthenticatedException("user.not_signed_in", "Sign in first."), 401, "user.not_signed_in" },
        { new AccessDeniedException("group.not_a_member", "Not a member."), 403, "group.not_a_member" },
        { new EntityNotFoundException("invitation.not_found", "Invitation not found."), 404, "invitation.not_found" },
        { new InvalidStateException("group.not_planning", "Not planning."), 409, "group.not_planning" },
        { new AlreadyExistsException("user.email_taken", "Email taken."), 409, "user.email_taken" },
        { new BusinessRuleViolationException("event.start_in_past", "In the past."), 422, "event.start_in_past" },
    };

    [Theory]
    [MemberData(nameof(Errors))]
    public async Task TryHandle_MapsExceptionToProblem(Exception exception, int status, string code)
    {
        var (response, body) = await HandleAsync(exception);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.ContentType);
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal(exception.Message, body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandle_WritesApiProblemShape()
    {
        var (_, body) = await HandleAsync(new InvalidStateException("group.not_planning", "Not planning."));

        Assert.Equal("Conflict", body.GetProperty("title").GetString());
        Assert.Equal("POST /api/groups", body.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("trace_id").GetString()));
        Assert.False(body.TryGetProperty("type", out _));
        Assert.False(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task TryHandle_MapsUniqueViolationToConflictWithoutDatabaseDetails()
    {
        var exception = new DbUpdateException(
            "Save failed.",
            new PostgresException("duplicate key violates \"ix_users_email\"", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        var (response, body) = await HandleAsync(exception);

        Assert.Equal(409, response.StatusCode);
        Assert.Equal("already_exists", body.GetProperty("code").GetString());
        Assert.DoesNotContain("ix_users_email", body.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TryHandle_HidesUnexpectedErrors()
    {
        var (response, body) = await HandleAsync(new InvalidOperationException("Connection string is secret."));

        Assert.Equal(500, response.StatusCode);
        Assert.Equal("internal_server_error", body.GetProperty("code").GetString());
        Assert.Equal("An unexpected error occurred.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ProblemDetailsService_FillsCodeFromStatusForFrameworkProblems()
    {
        await using var services = CreateServices();
        var context = CreateContext(services);
        context.Response.StatusCode = StatusCodes.Status404NotFound;

        await services.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails(),
        });

        var body = await ReadBodyAsync(context);
        Assert.Equal("not_found", body.GetProperty("code").GetString());
        Assert.False(body.TryGetProperty("type", out _));
    }

    private static async Task<(HttpResponse Response, JsonElement Body)> HandleAsync(Exception exception)
    {
        await using var services = CreateServices();
        var context = CreateContext(services);
        var handler = services.GetServices<IExceptionHandler>().OfType<GlobalExceptionHandler>().Single();

        Assert.True(await handler.TryHandleAsync(context, exception, CancellationToken.None));

        return (context.Response, await ReadBodyAsync(context));
    }

    private static ServiceProvider CreateServices()
    {
        return new ServiceCollection()
            .AddLogging()
            .AddApiProblemDetails()
            .BuildServiceProvider();
    }

    private static DefaultHttpContext CreateContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = services,
            Request =
            {
                Method = HttpMethods.Post, Path = "/api/groups"
            },
            Response = { Body = new MemoryStream() }
        };
        return context;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }
}
