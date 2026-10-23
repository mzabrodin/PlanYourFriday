using PlanYourFriday.Application.Events;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Application.Groups;

public sealed record TimeRangeView(DateTime Start, DateTime End)
{
    public static TimeRangeView From(TimeRange range)
    {
        ArgumentNullException.ThrowIfNull(range);
        return new TimeRangeView(range.Start, range.End);
    }
}

public sealed record GroupSummaryView(
    Guid Id,
    string Title,
    string? Description,
    string OrganizerName,
    GroupStatus Status,
    TimeRangeView Window,
    DateTime? AvailabilityDeadline,
    int MemberCount,
    IReadOnlyList<string> MemberNames,
    int ReadyCount,
    MemberRole MyRole,
    bool MyAvailabilityMarked,
    bool MyVotePending,
    DateTime? VotingDeadline,
    EventView? Event);

public sealed record GroupView(
    Guid Id,
    string Title,
    string? Description,
    Guid OrganizerId,
    string? InviteToken,
    TimeRangeView Window,
    int EventDurationMinutes,
    DateTime? AvailabilityDeadline,
    GroupStatus Status,
    MemberRole MyRole,
    IReadOnlyList<MemberView> Members,
    IReadOnlyList<InvitationView> Invitations,
    IReadOnlyList<TimeOptionView> Options,
    DateTime? VotingDeadline,
    EventView? Event);

public sealed record MemberView(
    Guid UserId,
    string DisplayName,
    string Email,
    string? PhotoUrl,
    MemberRole Role,
    bool HasAvailability);

public sealed record InvitationView(Guid Id, string Email, InvitationStatus Status);

public sealed record TimeOptionView(
    Guid Id,
    TimeRangeView Time,
    IReadOnlyList<Guid> FreeUserIds,
    IReadOnlyList<VoteView> Votes);

public sealed record VoteView(Guid UserId, VoteAnswer Answer);

public sealed record JoinGroupView(Guid GroupId, MemberView Member);

public sealed record MemberAvailabilityView(Guid UserId, IReadOnlyList<TimeRangeView> Slots);

public sealed record SuggestedTimeView(TimeRangeView Time, IReadOnlyList<Guid> FreeUserIds);
