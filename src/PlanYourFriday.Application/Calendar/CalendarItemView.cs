using PlanYourFriday.Application.Groups;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Application.Calendar;

public enum CalendarItemKind
{
    Event,
    Planning,
    MyFreeTime,
}

public sealed record CalendarItemView(
    string Id,
    CalendarItemKind Kind,
    Guid GroupId,
    string GroupTitle,
    string Title,
    TimeRangeView Time,
    string? Location,
    IReadOnlyList<string> People,
    string OrganizerName,
    GroupStatus GroupStatus);
