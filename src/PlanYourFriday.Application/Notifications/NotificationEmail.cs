using System.Globalization;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Notifications;

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

    public static EmailMessage Compose(
        NotificationKind kind,
        string groupTitle,
        string actorName,
        Uri link,
        IReadOnlyList<EmailDetail>? eventDetails)
    {
        ArgumentNullException.ThrowIfNull(link);

        var footer = "You're getting this because email notifications are on for your PlanYourFriday account.";
        var settings = new Uri(link, "/settings");
        var content = kind switch
        {
            NotificationKind.Invited => new EmailContent
            {
                Subject = $"{actorName} added you to \"{groupTitle}\"",
                Tone = EmailTone.Planning,
                Eyebrow = "New plan",
                Heading = $"{actorName} added you to \u201c{groupTitle}\u201d",
                Paragraphs = ["Mark when you're free so everyone can find a time. It takes about a minute."],
                ActionLabel = "Mark my free time",
                ActionUrl = link,
                Footer = footer,
                SettingsUrl = settings,
            },
            NotificationKind.DeadlineSoon => new EmailContent
            {
                Subject = $"Mark your free time for \"{groupTitle}\"",
                Tone = EmailTone.Planning,
                Eyebrow = "Deadline soon",
                Heading = $"Last call for \u201c{groupTitle}\u201d",
                Paragraphs =
                [
                    "The deadline to mark your free time is coming up. Add yours so the time that gets picked works for you too.",
                ],
                ActionLabel = "Mark my free time",
                ActionUrl = link,
                Footer = footer,
                SettingsUrl = settings,
            },
            NotificationKind.EventConfirmed => new EmailContent
            {
                Subject = $"\"{groupTitle}\" is on!",
                Tone = EmailTone.Scheduled,
                Eyebrow = "It's on",
                Heading = $"\u201c{groupTitle}\u201d is on!",
                Paragraphs =
                [
                    $"{actorName} confirmed the time. If your Google Calendar is connected, the event is already there.",
                ],
                Details = eventDetails ?? [],
                ActionLabel = "Open the plan",
                ActionUrl = link,
                Footer = footer,
                SettingsUrl = settings,
            },
            NotificationKind.EventCancelled => new EmailContent
            {
                Subject = $"\"{groupTitle}\" was cancelled",
                Tone = EmailTone.Cancelled,
                Eyebrow = "Cancelled",
                Heading = $"\u201c{groupTitle}\u201d was cancelled",
                Paragraphs = [$"{actorName} cancelled it. If it was in your Google Calendar, it's been taken out."],
                ActionLabel = "Open the event",
                ActionUrl = link,
                Footer = footer,
                SettingsUrl = settings,
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "This notification has no email."),
        };

        return EmailLayout.Render(content);
    }

    public static EmailMessage ComposeReminder(Event @event, string timeZoneId, Uri link)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(link);

        return EmailLayout.Render(new EmailContent
        {
            Subject = $"Reminder: \"{@event.Title}\" is coming up",
            Tone = EmailTone.Scheduled,
            Eyebrow = "Reminder",
            Heading = $"\u201c{@event.Title}\u201d is coming up",
            Paragraphs = ["A quick heads-up so it doesn't sneak up on you."],
            Details = Describe(@event, timeZoneId),
            ActionLabel = "Open the plan",
            ActionUrl = link,
            Footer = "You're getting this because you're going to this event on PlanYourFriday.",
            SettingsUrl = new Uri(link, "/settings"),
        });
    }

    public static IReadOnlyList<EmailDetail> Describe(Event @event, string timeZoneId)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var timeZone = TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var found) ? found : TimeZoneInfo.Utc;
        var start = TimeZoneInfo.ConvertTimeFromUtc(@event.Time.Start, timeZone);
        var end = TimeZoneInfo.ConvertTimeFromUtc(@event.Time.End, timeZone);
        var details = new List<EmailDetail>
        {
            new("When", string.Create(CultureInfo.InvariantCulture, $"{start:ddd d MMM, HH:mm} - {end:HH:mm} ({timeZone.Id})")),
        };

        if (@event.Location is not null)
        {
            details.Add(new EmailDetail("Where", @event.Location));
        }

        if (@event.MeetingLink is not null)
        {
            details.Add(new EmailDetail("Link", @event.MeetingLink));
        }

        if (@event.Description is not null)
        {
            details.Add(new EmailDetail("Notes", @event.Description));
        }

        return details;
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
