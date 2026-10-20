using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Events;

public sealed class CalendarEventLink : Entity
{
    private CalendarEventLink()
    {
    }

    internal CalendarEventLink(Guid eventId, Guid connectionId)
    {
        EventId = eventId;
        ConnectionId = connectionId;
        SyncStatus = SyncStatus.Pending;
    }

    public Guid EventId { get; private set; }

    public Guid ConnectionId { get; private set; }

    public string? ExternalEventId { get; private set; }

    public SyncStatus SyncStatus { get; private set; }

    internal void MarkSynced(string externalEventId)
    {
        ExternalEventId = externalEventId;
        SyncStatus = SyncStatus.Synced;
    }

    internal void MarkFailed()
    {
        SyncStatus = SyncStatus.Failed;
    }

    internal void MarkPending()
    {
        SyncStatus = SyncStatus.Pending;
    }
}
