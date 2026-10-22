using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Application.Calendar;

public sealed partial class CalendarSyncService(
    IEventRepository events,
    IUserRepository users,
    IExternalCalendarClient client,
    CalendarAccess access,
    IOptions<AppOptions> options,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<CalendarSyncService> logger)
{
    private const int BatchSize = 20;
    private static readonly TimeSpan RetryFailedAfter = TimeSpan.FromMinutes(15);

    public async Task<int> SyncPendingAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var pending = await events.ListNeedingSyncAsync(BatchSize, now - RetryFailedAfter, cancellationToken);
        foreach (var @event in pending)
        {
            await SyncAsync(@event, now, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return pending.Count;
    }

    private async Task SyncAsync(Event @event, DateTime now, CancellationToken cancellationToken)
    {
        var appLink = new Uri(options.Value.PublicUrl, $"/groups/{@event.GroupId}");
        foreach (var link in @event.CalendarLinks.Where(l => l.SyncStatus != SyncStatus.Synced).ToList())
        {
            try
            {
                var owner = await users.FindByCalendarConnectionAsync(link.ConnectionId, cancellationToken)
                    ?? throw new InvalidStateException("calendar_connection.not_found", "Calendar connection not found.");
                var connection = owner.CalendarConnections.First(c => c.Id == link.ConnectionId);
                var token = await access.GetAccessTokenAsync(owner, connection, cancellationToken);

                if (@event.Status == EventStatus.Confirmed)
                {
                    var externalId = await client.SaveEventAsync(
                        token,
                        connection.CalendarId,
                        link.ExternalEventId,
                        new ExternalEvent(@event.Title, @event.Description, @event.Location, @event.MeetingLink, @event.Time, appLink),
                        cancellationToken);
                    @event.MarkSynced(link.ConnectionId, externalId);
                }
                else
                {
                    if (link.ExternalEventId is not null)
                    {
                        await client.DeleteEventAsync(token, connection.CalendarId, link.ExternalEventId, cancellationToken);
                    }

                    @event.MarkRemovedFromCalendar(link.ConnectionId);
                }

                owner.MarkCalendarSynced(connection.Id, now);
            }
            catch (Exception exception) when (exception is HttpRequestException or DomainException or CalendarAuthorizationException)
            {
                LogSyncFailed(logger, exception, @event.Id, link.ConnectionId);
                @event.MarkSyncFailed(link.ConnectionId);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Calendar sync of event {EventId} with connection {ConnectionId} failed")]
    private static partial void LogSyncFailed(ILogger logger, Exception exception, Guid eventId, Guid connectionId);
}
