using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Application.Notifications;

public sealed class NotificationPublisher(INotificationRepository notifications, IOutbox outbox)
{
    public void NotifyMembers(Group group, NotificationKind kind, Guid? actorId)
    {
        ArgumentNullException.ThrowIfNull(group);

        Notify(group.Members.Select(m => m.UserId), kind, group.Id, actorId);
    }

    public void Notify(IEnumerable<Guid> userIds, NotificationKind kind, Guid groupId, Guid? actorId)
    {
        foreach (var userId in userIds.Where(id => id != actorId).Distinct())
        {
            var notification = Notification.ForGroup(userId, kind, groupId, actorId);
            notifications.Add(notification);
            if (NotificationEmail.ExistsFor(kind))
            {
                outbox.Add(new NotificationEmailRequested(notification.Id).ToOutboxMessage());
            }
        }
    }

    public void NotifyInApp(Guid userId, NotificationKind kind, Guid groupId, Guid? actorId)
    {
        notifications.Add(Notification.ForGroup(userId, kind, groupId, actorId));
    }

    public void NotifyParticipants(Poll poll, NotificationKind kind, Guid? actorId)
    {
        ArgumentNullException.ThrowIfNull(poll);

        foreach (var participant in poll.Participants.Where(p => p.UserId != actorId))
        {
            notifications.Add(Notification.ForPoll(participant.UserId, kind, poll.Id, actorId));
        }
    }

    public void NotifyFriend(Guid userId, NotificationKind kind, Guid actorId)
    {
        notifications.Add(Notification.ForFriend(userId, kind, actorId));
    }
}
