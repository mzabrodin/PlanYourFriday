using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Api.Users;

public sealed record UserSettingsRequest(
    [Required]
    [AllowedValues("system", "light", "dark")]
    string Theme,
    [Required]
    [AllowedValues("24h", "12h")]
    string TimeFormat,
    [Range(0, 1)]
    int WeekStartsOn,
    [Range(15, 24 * 60)]
    int DefaultEventDurationMinutes,
    [Required]
    UsualFreeTimeRequest UsualFreeTime,
    [Required]
    NotificationSettingsRequest Notifications)
{
    public UserSettings ToSettings()
    {
        return new UserSettings(
            Theme,
            TimeFormat,
            WeekStartsOn,
            DefaultEventDurationMinutes,
            new UsualFreeTime(UsualFreeTime.Weekdays?.ToHours(), UsualFreeTime.Weekends?.ToHours()),
            new NotificationSettings(
                Notifications.Invitations,
                Notifications.DeadlineReminders,
                Notifications.EventConfirmed,
                Notifications.EventCancelled,
                Notifications.EventReminder,
                Notifications.EventReminderHours));
    }
}

public sealed record UsualFreeTimeRequest(DailyHoursRequest? Weekdays, DailyHoursRequest? Weekends);

public sealed record DailyHoursRequest(
    [Required]
    [RegularExpression(DailyHoursRequest.TimeOfDay)]
    string Start,
    [Required]
    [RegularExpression(DailyHoursRequest.TimeOfDay)]
    string End)
{
    private const string TimeOfDay = "^([01][0-9]|2[0-3]):[0-5][0-9]$";

    public DailyHours ToHours()
    {
        return new DailyHours(Start, End);
    }
}

public sealed record NotificationSettingsRequest(
    bool Invitations,
    bool DeadlineReminders,
    bool EventConfirmed,
    bool EventCancelled,
    bool EventReminder,
    [Range(1, 7 * 24)] int EventReminderHours);
