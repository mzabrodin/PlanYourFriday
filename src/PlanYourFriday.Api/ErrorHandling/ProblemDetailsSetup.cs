using System.Diagnostics;

using Microsoft.AspNetCore.WebUtilities;

namespace PlanYourFriday.Api.ErrorHandling;

internal static class ProblemDetailsSetup
{
    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var request = context.HttpContext.Request;

        problem.Type = null;
        problem.Instance = $"{request.Method} {request.Path}";

        var phrase = ReasonPhrases.GetReasonPhrase(problem.Status ?? context.HttpContext.Response.StatusCode);
        if (phrase.Length > 0)
        {
            problem.Extensions.TryAdd("code", phrase.Replace(' ', '_').ToLowerInvariant());
        }

        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
    }
}
