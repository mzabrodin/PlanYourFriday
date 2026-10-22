using System.Net;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Requests;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Domain.Common;

using GoogleCalendar = Google.Apis.Calendar.v3.CalendarService;

namespace PlanYourFriday.Infrastructure.Google;

internal sealed class GoogleCalendarClient(GoogleCalendar calendar) : IExternalCalendarClient
{
    private const string DefaultColor = "#4285f4";

    public async Task<IReadOnlyList<ExternalCalendar>> ListCalendarsAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        var request = Authorized(calendar.CalendarList.List(), accessToken);
        request.MinAccessRole = CalendarListResource.ListRequest.MinAccessRoleEnum.Reader;
        var list = await CallAsync(() => request.ExecuteAsync(cancellationToken));

        return
        [
            .. (list.Items ?? []).Select(c => new ExternalCalendar(
                c.Id,
                c.SummaryOverride ?? c.Summary ?? c.Id,
                c.BackgroundColor ?? DefaultColor,
                c.Primary ?? false)),
        ];
    }

    public async Task<IReadOnlyList<TimeRange>> GetBusyAsync(
        string accessToken,
        IReadOnlyCollection<string> calendarIds,
        TimeRange range,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calendarIds);
        ArgumentNullException.ThrowIfNull(range);

        if (calendarIds.Count == 0)
        {
            return [];
        }

        var request = Authorized(
            calendar.Freebusy.Query(new FreeBusyRequest
            {
                TimeMinDateTimeOffset = range.Start,
                TimeMaxDateTimeOffset = range.End,
                Items = [.. calendarIds.Select(id => new FreeBusyRequestItem { Id = id })],
            }),
            accessToken);
        var response = await CallAsync(() => request.ExecuteAsync(cancellationToken));

        return
        [
            .. (response.Calendars?.Values ?? [])
            .SelectMany(c => c.Busy ?? [])
            .Where(b => b.StartDateTimeOffset is not null && b.EndDateTimeOffset > b.StartDateTimeOffset)
            .Select(b => new TimeRange(b.StartDateTimeOffset!.Value.UtcDateTime, b.EndDateTimeOffset!.Value.UtcDateTime)),
        ];
    }

    public async Task<string> SaveEventAsync(
        string accessToken,
        string calendarId,
        string? externalEventId,
        ExternalEvent @event,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var body = new Event
        {
            Summary = @event.Title,
            Description = Describe(@event),
            Location = @event.Location,
            Start = new EventDateTime { DateTimeDateTimeOffset = @event.Time.Start },
            End = new EventDateTime { DateTimeDateTimeOffset = @event.Time.End },
        };

        if (externalEventId is not null)
        {
            var update = Authorized(calendar.Events.Update(body, calendarId, externalEventId), accessToken);
            try
            {
                return (await CallAsync(() => update.ExecuteAsync(cancellationToken))).Id;
            }
            catch (HttpRequestException exception) when (IsGone(exception))
            {
            }
        }

        var insert = Authorized(calendar.Events.Insert(body, calendarId), accessToken);
        return (await CallAsync(() => insert.ExecuteAsync(cancellationToken))).Id;
    }

    public async Task DeleteEventAsync(
        string accessToken,
        string calendarId,
        string externalEventId,
        CancellationToken cancellationToken)
    {
        var request = Authorized(calendar.Events.Delete(calendarId, externalEventId), accessToken);
        try
        {
            await CallAsync(() => request.ExecuteAsync(cancellationToken));
        }
        catch (HttpRequestException exception) when (IsGone(exception))
        {
        }
    }

    private static TRequest Authorized<TRequest>(TRequest request, string accessToken)
        where TRequest : ClientServiceRequest
    {
        request.Credential = GoogleCredential.FromAccessToken(accessToken);
        return request;
    }

    private static bool IsGone(HttpRequestException exception)
    {
        return exception.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone;
    }

    private static async Task<T> CallAsync<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (GoogleApiException exception) when (exception.HttpStatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidStateException(
                "calendar_connection.unauthorized",
                "Google rejected access to the calendar. Connect it again.");
        }
        catch (GoogleApiException exception)
        {
            throw new HttpRequestException(exception.Message, exception, exception.HttpStatusCode);
        }
    }

    private static string Describe(ExternalEvent @event)
    {
        string?[] parts = [@event.Description, @event.MeetingLink, $"Planned with Plan Your Friday: {@event.AppLink}"];
        return string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }
}
