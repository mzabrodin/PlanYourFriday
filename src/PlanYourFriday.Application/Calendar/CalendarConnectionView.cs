using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Calendar;

public sealed record CalendarConnectionView(
    Guid Id,
    CalendarProvider Provider,
    string AccountEmail,
    ConnectionStatus Status,
    IReadOnlyList<ExternalCalendar> Calendars,
    IReadOnlyList<string> BusyCalendarIds,
    string WriteCalendarId,
    DateTime? LastSyncedAt)
{
    public static CalendarConnectionView From(CalendarConnection connection, IReadOnlyList<ExternalCalendar> calendars)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return new CalendarConnectionView(
            connection.Id,
            connection.Provider,
            connection.AccountEmail,
            connection.Status,
            calendars,
            connection.BusyCalendarIds,
            connection.CalendarId,
            connection.LastSyncedAt);
    }
}

public sealed record GoogleCalendarGrant(
    string Subject,
    string Email,
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt,
    IReadOnlyList<string> Scopes);
