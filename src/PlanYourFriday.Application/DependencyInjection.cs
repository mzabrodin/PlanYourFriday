using Microsoft.Extensions.DependencyInjection;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;
using PlanYourFriday.Application.Users;

namespace PlanYourFriday.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ExternalSignInService>();
        services.AddScoped<AccountService>();
        services.AddScoped<GroupService>();
        services.AddScoped<EventService>();
        services.AddScoped<CalendarService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<FriendService>();
        services.AddScoped<PollService>();
        services.AddScoped<NotificationPublisher>();
        services.AddScoped<ReminderService>();
        services.AddScoped<CalendarAccess>();
        services.AddScoped<CalendarConnectionService>();
        services.AddScoped<CalendarSyncService>();
        services.AddScoped<OutboxDispatcher>();
        services.AddScoped<IOutboxHandler, InvitationEmailHandler>();
        services.AddScoped<IOutboxHandler, NotificationEmailHandler>();
        services.AddScoped<IOutboxHandler, EventReminderEmailHandler>();
        services.AddScoped<IOutboxHandler, CalendarEventRemovalHandler>();

        return services;
    }
}
