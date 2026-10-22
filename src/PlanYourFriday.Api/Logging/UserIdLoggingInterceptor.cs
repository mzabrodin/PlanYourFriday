using System.Security.Claims;

using Microsoft.AspNetCore.HttpLogging;

namespace PlanYourFriday.Api.Logging;

internal sealed class UserIdLoggingInterceptor : IHttpLoggingInterceptor
{
    public ValueTask OnRequestAsync(HttpLoggingInterceptorContext logContext)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask OnResponseAsync(HttpLoggingInterceptorContext logContext)
    {
        var userId = logContext.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            logContext.AddParameter("UserId", userId);
        }

        return ValueTask.CompletedTask;
    }
}
