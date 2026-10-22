using System.Globalization;

using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Notifications;

public sealed record EmailMessage(string Subject, string Body);

public static class NotificationEmail
{
    public static bool ExistsFor(NotificationKind kind)
    {
        return PreferenceFor(kind) is not null;
    }

    public static bool IsWanted(NotificationSettings settings, NotificationKind kind)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return PreferenceFor(kind)?.Invoke(settings) ?? false;
    }

    public static EmailMessage Compose(NotificationKind kind, string groupTitle, string actorName, Uri link, string? eventDetails)
    {
        return kind switch
        {
            NotificationKind.Invited => new EmailMessage(
                $"{actorName} added you to \"{groupTitle}\"",
                $"{actorName} added you to \"{groupTitle}\" on Plan Your Friday.\nMark when you're free so everyone can find a time:\n\n{link}"),
            NotificationKind.DeadlineSoon => new EmailMessage(
                $"Mark your free time for \"{groupTitle}\"",
                $"The deadline to mark your free time for \"{groupTitle}\" is coming up. It takes a minute:\n\n{link}"),
            NotificationKind.EventConfirmed => new EmailMessage(
                $"\"{groupTitle}\" is on!",
                $"{actorName} confirmed the time for \"{groupTitle}\".\n\n{eventDetails}\n\nDetails: {link}"),
            NotificationKind.EventCancelled => new EmailMessage(
                $"\"{groupTitle}\" was cancelled",
                $"{actorName} cancelled the event for \"{groupTitle}\". The group is back to picking a time:\n\n{link}"),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "This notification has no email."),
        };
    }

    public static EmailMessage ComposeReminder(Event @event, string timeZoneId, Uri link)
    {
        ArgumentNullException.ThrowIfNull(@event);

        return new EmailMessage(
            $"Reminder: \"{@event.Title}\" is coming up",
            $"{Describe(@event, timeZoneId)}\n\n{link}");
    }

    public static string Describe(Event @event, string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var found) ? found : TimeZoneInfo.Utc;
        var start = TimeZoneInfo.ConvertTimeFromUtc(@event.Time.Start, timeZone);
        var end = TimeZoneInfo.ConvertTimeFromUtc(@event.Time.End, timeZone);
        var lines = new List<string>
        {
            string.Create(CultureInfo.InvariantCulture, $"When: {start:ddd d MMM, HH:mm} - {end:HH:mm} ({timeZone.Id})"),
        };

        if (@event.Location is not null)
        {
            lines.Add($"Where: {@event.Location}");
        }

        if (@event.MeetingLink is not null)
        {
            lines.Add($"Link: {@event.MeetingLink}");
        }

        return string.Join('\n', lines);
    }

    private static Func<NotificationSettings, bool>? PreferenceFor(NotificationKind kind)
    {
        return kind switch
        {
            NotificationKind.Invited => settings => settings.Invitations,
            NotificationKind.DeadlineSoon => settings => settings.DeadlineReminders,
            NotificationKind.EventConfirmed => settings => settings.EventConfirmed,
            NotificationKind.EventCancelled => settings => settings.EventCancelled,
            _ => null,
        };
    }
}
