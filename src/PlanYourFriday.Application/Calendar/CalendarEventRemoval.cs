using System.Text.Json;

using Microsoft.Extensions.Logging;

using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Application.Calendar;

/// <summary>
/// Removes an event from someone's Google Calendar after the event itself is gone from the database,
/// e.g. when its group is deleted. The external ids are captured before the delete, one message per calendar.
/// </summary>
public sealed record CalendarEventRemovalRequested(Guid ConnectionId, string ExternalEventId)
{
    public const string Type = "calendar.event_removal_requested";

    /// <summary>
    /// Queues removal of the copies of <paramref name="events"/> that should not stay in calendars:
    /// cancelled events and the ones still to come. Past events stay as history.
    /// </summary>
    public static void QueueFor(IOutbox outbox, IEnumerable<Event> events, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events.Where(e => e.Status == EventStatus.Cancelled || e.Time.End > now))
        {
            foreach (var link in @event.CalendarLinks.Where(l => l.ExternalEventId is not null))
            {
                outbox.Add(new CalendarEventRemovalRequested(link.ConnectionId, link.ExternalEventId!).ToOutboxMessage());
            }
        }
    }

    public OutboxMessage ToOutboxMessage()
    {
        return OutboxMessage.Create(Type, JsonSerializer.Serialize(this));
    }
}

public sealed partial class CalendarEventRemovalHandler(
    IUserRepository users,
    IExternalCalendarClient client,
    CalendarAccess access,
    ILogger<CalendarEventRemovalHandler> logger) : IOutboxHandler
{
    public string Type => CalendarEventRemovalRequested.Type;

    public async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<CalendarEventRemovalRequested>(payload)
            ?? throw new JsonException("Empty calendar event removal payload.");
        var owner = await users.FindByCalendarConnectionAsync(request.ConnectionId, cancellationToken);
        var connection = owner?.CalendarConnections.FirstOrDefault(c => c.Id == request.ConnectionId);
        if (owner is null || connection is null)
        {
            return;
        }

        try
        {
            var token = await access.GetAccessTokenAsync(owner, connection, cancellationToken);
            await client.DeleteEventAsync(token, connection.CalendarId, request.ExternalEventId, cancellationToken);
        }
        catch (Exception exception) when (exception is DomainException or CalendarAuthorizationException)
        {
            // The calendar was disconnected or access was revoked: there is nothing we are allowed to remove.
            LogSkipped(logger, exception, request.ConnectionId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Skipped removing an event from calendar connection {ConnectionId}")]
    private static partial void LogSkipped(ILogger logger, Exception exception, Guid connectionId);
}
