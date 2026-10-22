using PlanYourFriday.Application.Groups;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Application.Events;

public enum CalendarSyncState
{
    Synced,
    Pending,
    Failed,
    NotConnected,
}

public sealed record EventView(
    Guid Id,
    string Title,
    TimeRangeView Time,
    string Location,
    string? MeetingLink,
    string? Description,
    EventStatus Status,
    CalendarSyncState MyCalendarSync);
