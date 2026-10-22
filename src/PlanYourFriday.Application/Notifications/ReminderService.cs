using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Application.Notifications;

public sealed class ReminderService(
    IGroupRepository groups,
    IEventRepository events,
    IUserRepository users,
    INotificationRepository notifications,
    NotificationPublisher publisher,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan DeadlineReminderWindow = TimeSpan.FromHours(24);
    private static readonly TimeSpan MaxEventReminderLead = TimeSpan.FromDays(7);

    public async Task<int> RemindAboutDeadlinesAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var created = 0;
        foreach (var group in await groups.ListWithDeadlineBetweenAsync(now, now + DeadlineReminderWindow, cancellationToken))
        {
            foreach (var member in group.Members.Where(m => m.Availability.Count == 0))
            {
                if (await notifications.ExistsAsync(member.UserId, group.Id, NotificationKind.DeadlineSoon,
                    cancellationToken))
                {
                    continue;
                }

                publisher.Notify([member.UserId], NotificationKind.DeadlineSoon, group.Id, null);
                created++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<int> RemindAboutEventsAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var sent = 0;
        foreach (var @event in await events.ListStartingBetweenAsync(now, now + MaxEventReminderLead, cancellationToken))
        {
            var group = await groups.GetByIdAsync(@event.GroupId, cancellationToken);
            if (group is null)
            {
                continue;
            }

            var pending = group.Members.Where(m => m.RemindedEventId != @event.Id).ToList();
            var settings = (await users.ListByIdsAsync([.. pending.Select(m => m.UserId)], cancellationToken))
                .ToDictionary(u => u.Id, u => u.Settings.Notifications);
            var due = pending
                .Where(m => settings.TryGetValue(m.UserId, out var wanted)
                    && wanted.EventReminder
                    && @event.Time.Start - now <= TimeSpan.FromHours(wanted.EventReminderHours))
                .ToList();

            foreach (var member in due)
            {
                outbox.Add(new EventReminderEmailRequested(@event.Id, member.UserId).ToOutboxMessage());
                group.MarkEventReminderSent(member.UserId, @event.Id);
            }

            sent += due.Count;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return sent;
    }
}
