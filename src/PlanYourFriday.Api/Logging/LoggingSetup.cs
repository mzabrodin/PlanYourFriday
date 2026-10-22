using Microsoft.AspNetCore.HttpLogging;

namespace PlanYourFriday.Api.Logging;

internal static class LoggingSetup
{
    public static IServiceCollection AddApiLogging(this IServiceCollection services)
    {
        services.AddHttpLogging(options =>
        {
            options.LoggingFields = HttpLoggingFields.RequestMethod
                | HttpLoggingFields.RequestPath
                | HttpLoggingFields.ResponseStatusCode
                | HttpLoggingFields.Duration;
            options.CombineLogs = true;
        });
        services.AddHttpLoggingInterceptor<UserIdLoggingInterceptor>();

        return services;
    }
}
