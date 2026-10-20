using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Users;
using PlanYourFriday.Infrastructure.Persistence;

namespace PlanYourFriday.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql
                .MapEnum<AuthProvider>("auth_provider")
                .MapEnum<CalendarProvider>("calendar_provider")
                .MapEnum<ConnectionStatus>("connection_status")
                .MapEnum<GroupStatus>("group_status")
                .MapEnum<MemberRole>("member_role")
                .MapEnum<InvitationStatus>("invitation_status")
                .MapEnum<EventStatus>("event_status")
                .MapEnum<SyncStatus>("sync_status"))
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
