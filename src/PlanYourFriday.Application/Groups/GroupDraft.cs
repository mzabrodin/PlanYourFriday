using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Application.Groups;

public sealed record GroupDraft(
    string Title,
    string? Description,
    TimeRange Window,
    TimeSpan EventDuration,
    DateTime? AvailabilityDeadline,
    IReadOnlyCollection<Guid> MemberIds,
    Guid? PollId);
