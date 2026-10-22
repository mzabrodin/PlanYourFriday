using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;
using PlanYourFriday.Infrastructure.Email;
using PlanYourFriday.Infrastructure.Google;
using PlanYourFriday.Infrastructure.Persistence;
using PlanYourFriday.Infrastructure.Persistence.Queries;
using PlanYourFriday.Infrastructure.Persistence.Repositories;

using GoogleCalendar = Google.Apis.Calendar.v3.CalendarService;

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
                .MapEnum<VoteAnswer>("vote_answer")
                .MapEnum<NotificationKind>("notification_kind")
                .MapEnum<EventStatus>("event_status")
                .MapEnum<SyncStatus>("sync_status")
                .MapEnum<FriendshipStatus>("friendship_status"))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IFriendshipRepository, FriendshipRepository>();
        services.AddScoped<IPollRepository, PollRepository>();
        services.AddScoped<GroupQueries>();
        services.AddScoped<IGroupQueries>(provider => provider.GetRequiredService<GroupQueries>());
        services.AddScoped<ICalendarQueries>(provider => provider.GetRequiredService<GroupQueries>());
        services.AddScoped<IAccountQueries, AccountQueries>();
        services.AddScoped<INotificationQueries, NotificationQueries>();
        services.AddScoped<IFriendQueries, FriendQueries>();
        services.AddScoped<IPollQueries, PollQueries>();
        services.AddScoped<IOutbox, OutboxWriter>();

        services.Configure<AppOptions>(configuration.GetSection(AppOptions.Section));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.Section));
        services.Configure<GoogleCalendarOptions>(configuration.GetSection("Authentication:Google"));
        services.AddSingleton(_ => new GoogleCalendar(new BaseClientService.Initializer { ApplicationName = "PlanYourFriday" }));
        services.AddSingleton(provider =>
        {
            var google = provider.GetRequiredService<IOptions<GoogleCalendarOptions>>().Value;
            return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets { ClientId = google.ClientId, ClientSecret = google.ClientSecret },
            });
        });
        services.AddSingleton<IExternalCalendarClient, GoogleCalendarClient>();
        services.AddSingleton<ICalendarTokenClient, GoogleTokenClient>();
        services.AddSingleton<ITokenProtector, DataProtectionTokenProtector>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IOutboxStore, OutboxStore>();

        services.AddDataProtection()
            .SetApplicationName("PlanYourFriday")
            .PersistKeysToDbContext<AppDbContext>();

        return services;
    }
}
