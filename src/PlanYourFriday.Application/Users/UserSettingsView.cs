using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed record UserSettingsView(
    string Theme,
    string TimeFormat,
    int WeekStartsOn,
    int DefaultEventDurationMinutes,
    UsualFreeTimeView UsualFreeTime,
    NotificationSettingsView Notifications)
{
    public static UserSettingsView From(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var notifications = settings.Notifications;
        return new UserSettingsView(
            settings.Theme,
            settings.TimeFormat,
            settings.WeekStartsOn,
            settings.DefaultEventDurationMinutes,
            new UsualFreeTimeView(
                DailyHoursView.From(settings.UsualFreeTime.Weekdays),
                DailyHoursView.From(settings.UsualFreeTime.Weekends)),
            new NotificationSettingsView(
                notifications.Invitations,
                notifications.DeadlineReminders,
                notifications.EventConfirmed,
                notifications.EventCancelled,
                notifications.EventReminder,
                notifications.EventReminderHours));
    }
}

public sealed record UsualFreeTimeView(DailyHoursView? Weekdays, DailyHoursView? Weekends);

public sealed record DailyHoursView(string Start, string End)
{
    public static DailyHoursView? From(DailyHours? hours)
    {
        return hours is null ? null : new DailyHoursView(hours.Start, hours.End);
    }
}

public sealed record NotificationSettingsView(
    bool Invitations,
    bool DeadlineReminders,
    bool EventConfirmed,
    bool EventCancelled,
    bool EventReminder,
    int EventReminderHours);
