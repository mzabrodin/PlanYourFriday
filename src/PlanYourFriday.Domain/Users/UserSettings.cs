namespace PlanYourFriday.Domain.Users;

public sealed record UserSettings(
    string Theme,
    string TimeFormat,
    int WeekStartsOn,
    int DefaultEventDurationMinutes,
    UsualFreeTime UsualFreeTime,
    NotificationSettings Notifications)
{
    public static UserSettings Default { get; } = new(
        "system",
        "24h",
        1,
        120,
        new UsualFreeTime(new DailyHours("18:00", "23:00"), new DailyHours("12:00", "23:00")),
        new NotificationSettings(true, true, true, true, true, 24));
}

public sealed record UsualFreeTime(DailyHours? Weekdays, DailyHours? Weekends);

public sealed record DailyHours(string Start, string End);

public sealed record NotificationSettings(
    bool Invitations,
    bool DeadlineReminders,
    bool EventConfirmed,
    bool EventCancelled,
    bool EventReminder,
    int EventReminderHours
);
