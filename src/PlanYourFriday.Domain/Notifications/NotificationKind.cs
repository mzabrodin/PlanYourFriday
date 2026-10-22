namespace PlanYourFriday.Domain.Notifications;

public enum NotificationKind
{
    Invited,
    MemberJoined,
    VotingStarted,
    DeadlineSoon,
    EventConfirmed,
    EventUpdated,
    EventCancelled,
    FriendRequest,
    FriendAccepted,
    PollStarted,
    PollClosed,
}
