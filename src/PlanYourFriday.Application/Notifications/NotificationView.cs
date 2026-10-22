using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Application.Notifications;

public sealed record NotificationView(
    Guid Id,
    NotificationKind Kind,
    Guid? GroupId,
    string? GroupTitle,
    Guid? PollId,
    string? PollTitle,
    string? ActorName,
    DateTime CreatedAt,
    bool Read);
