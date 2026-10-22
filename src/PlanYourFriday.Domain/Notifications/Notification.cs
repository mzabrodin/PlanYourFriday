using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Notifications;

public sealed class Notification : Entity
{
    private Notification()
    {
    }

    private Notification(Guid userId, NotificationKind kind, Guid? actorId, Guid? groupId, Guid? pollId)
    {
        UserId = userId;
        Kind = kind;
        ActorId = actorId;
        GroupId = groupId;
        PollId = pollId;
    }

    public Guid UserId { get; private set; }

    public NotificationKind Kind { get; private set; }

    public Guid? ActorId { get; private set; }

    public Guid? GroupId { get; private set; }

    public Guid? PollId { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public static Notification ForGroup(Guid userId, NotificationKind kind, Guid groupId, Guid? actorId)
    {
        return new Notification(userId, kind, actorId, groupId, null);
    }

    public static Notification ForPoll(Guid userId, NotificationKind kind, Guid pollId, Guid? actorId)
    {
        return new Notification(userId, kind, actorId, null, pollId);
    }

    public static Notification ForFriend(Guid userId, NotificationKind kind, Guid actorId)
    {
        return new Notification(userId, kind, actorId, null, null);
    }

    public void MarkRead(DateTime now)
    {
        ReadAt ??= now;
    }
}
