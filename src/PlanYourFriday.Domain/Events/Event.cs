using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Events;

public sealed class Event : Entity
{
    private readonly List<CalendarEventLink> _calendarLinks = [];

    private Event()
    {
    }

    internal Event(
        Guid groupId,
        Guid ownerId,
        string title,
        string? description,
        TimeRange time,
        string? location,
        string? meetingLink)
    {
        GroupId = groupId;
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Time = time;
        Location = location;
        MeetingLink = meetingLink;
        Status = EventStatus.Confirmed;
    }

    public Guid GroupId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public TimeRange Time { get; private set; } = null!;

    public string? Location { get; private set; }

    public string? MeetingLink { get; private set; }

    public EventStatus Status { get; private set; }

    public IReadOnlyCollection<CalendarEventLink> CalendarLinks => _calendarLinks;

    public CalendarEventLink AddCalendarLink(Guid connectionId)
    {
        var existing = _calendarLinks.FirstOrDefault(l => l.ConnectionId == connectionId);
        if (existing is not null)
        {
            return existing;
        }

        var link = new CalendarEventLink(Id, connectionId);
        _calendarLinks.Add(link);
        return link;
    }

    public void MarkSynced(Guid connectionId, string externalEventId)
    {
        FindLink(connectionId).MarkSynced(externalEventId);
    }

    public void MarkSyncFailed(Guid connectionId)
    {
        FindLink(connectionId).MarkFailed();
    }

    internal void Cancel()
    {
        if (Status == EventStatus.Cancelled)
        {
            return;
        }

        Status = EventStatus.Cancelled;
        foreach (var link in _calendarLinks)
        {
            link.MarkPending();
        }
    }

    private CalendarEventLink FindLink(Guid connectionId)
    {
        return _calendarLinks.FirstOrDefault(l => l.ConnectionId == connectionId)
            ?? throw new EntityNotFoundException("calendar_link.not_found", "Calendar link not found.");
    }
}
