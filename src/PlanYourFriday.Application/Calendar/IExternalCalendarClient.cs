using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Application.Calendar;

public interface IExternalCalendarClient
{
    Task<IReadOnlyList<ExternalCalendar>> ListCalendarsAsync(string accessToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<TimeRange>> GetBusyAsync(
        string accessToken,
        IReadOnlyCollection<string> calendarIds,
        TimeRange range,
        CancellationToken cancellationToken);

    Task<string> SaveEventAsync(
        string accessToken,
        string calendarId,
        string? externalEventId,
        ExternalEvent @event,
        CancellationToken cancellationToken);

    Task DeleteEventAsync(
        string accessToken,
        string calendarId,
        string externalEventId,
        CancellationToken cancellationToken);
}

public sealed record ExternalCalendar(string Id, string Name, string Color, bool Primary);

public sealed record ExternalEvent(
    string Title,
    string? Description,
    string? Location,
    string? MeetingLink,
    TimeRange Time,
    Uri AppLink);
