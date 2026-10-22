using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed record AccountExport(
    DateTime ExportedAt,
    ExportedProfile Profile,
    UserSettingsView Settings,
    IReadOnlyList<ExportedCalendarConnection> CalendarConnections,
    IReadOnlyList<ExportedGroup> Groups,
    IReadOnlyList<ExportedFriend> Friends,
    IReadOnlyList<ExportedPendingFriendship> FriendRequests,
    IReadOnlyList<ExportedPoll> Polls,
    IReadOnlyList<ExportedNotification> Notifications
);

public sealed record ExportedProfile(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string? PhotoUrl,
    string TimeZone,
    string FriendCode,
    DateTime CreatedAt,
    IReadOnlyList<AuthProvider> SignInProviders
);

public sealed record ExportedCalendarConnection(
    CalendarProvider Provider,
    string AccountEmail,
    ConnectionStatus Status,
    DateTime ConnectedAt
);

public sealed record ExportedGroup(
    Guid Id,
    string Title,
    MemberRole Role,
    TimeRangeView Window,
    GroupStatus Status,
    EventView? Event,
    IReadOnlyList<TimeRangeView> MyAvailability,
    IReadOnlyList<ExportedTimeVote> MyVotes
);

public sealed record ExportedTimeVote(TimeRangeView Time, VoteAnswer Answer);

public sealed record ExportedFriend(string DisplayName, string Username, DateTime Since);

public sealed record ExportedPendingFriendship(string DisplayName, string Username, RequestDirection Direction, DateTime CreatedAt);

public sealed record ExportedPoll(
    Guid Id,
    string Title,
    bool CreatedByMe,
    DateTime? Deadline,
    DateTime? ClosedAt,
    IReadOnlyList<string> Options,
    IReadOnlyList<string> MyVotes
);

public sealed record ExportedNotification(NotificationKind Kind, DateTime CreatedAt, bool Read);
