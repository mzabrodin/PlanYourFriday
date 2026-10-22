using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Notifications;

public class NotificationTests
{
    private readonly IGroupRepository _groups = Substitute.For<IGroupRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly User _organizer = CreateUser("organizer@example.com");
    private readonly User _ready = CreateUser("ready@example.com");
    private readonly User _late = CreateUser("late@example.com");

    private static User CreateUser(string email)
    {
        return User.Create(email, email, "UTC", AuthProvider.Google, email);
    }

    private ReminderService Reminders(int day, int hour)
    {
        return new ReminderService(
            _groups, _events, _users, _notifications, new NotificationPublisher(_notifications, _outbox), _outbox, _unitOfWork, Clock(day, hour));
    }

    private Group CreateGroup(DateTime? deadline = null)
    {
        var group = Group.Create(_organizer.Id, "Board games", null, Range(3, 10, 22), TimeSpan.FromHours(2), deadline, At(1, 0));
        group.AddMember(_ready.Id);
        group.AddMember(_late.Id);
        group.SetAvailability(_ready.Id, [Range(3, 10, 12)], At(1, 0));
        _groups.GetByIdAsync(group.Id, Arg.Any<CancellationToken>()).Returns(group);
        return group;
    }

    [Fact]
    public void Notifier_SkipsTheActorAndEmailsOnlyKindsWithAnEmail()
    {
        var group = CreateGroup();
        var publisher = new NotificationPublisher(_notifications, _outbox);

        publisher.NotifyMembers(group, NotificationKind.EventConfirmed, _organizer.Id);
        publisher.NotifyMembers(group, NotificationKind.EventUpdated, _organizer.Id);

        _notifications.Received(4).Add(Arg.Is<Notification>(n => n.UserId != _organizer.Id));
        _outbox.Received(2).Add(Arg.Is<OutboxMessage>(m => m.Type == NotificationEmailRequested.Type));
    }

    [Fact]
    public async Task DeadlineReminders_NotifyMembersWithoutSlotsOnce()
    {
        var group = CreateGroup(deadline: At(2, 6));
        _groups.ListWithDeadlineBetweenAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([group]);
        _notifications.ExistsAsync(_organizer.Id, group.Id, NotificationKind.DeadlineSoon, Arg.Any<CancellationToken>())
            .Returns(true);

        var created = await Reminders(1, 12).RemindAboutDeadlinesAsync(CancellationToken.None);

        Assert.Equal(1, created);
        _notifications.Received(1)
            .Add(Arg.Is<Notification>(n =>
                n.UserId == _late.Id && n.Kind == NotificationKind.DeadlineSoon));
    }

    [Fact]
    public async Task EventReminders_RespectEachMembersLeadTimeAndAreSentOnce()
    {
        var group = CreateGroup();
        var @event = group.ScheduleEvent("Games", null, Range(3, 18, 20), null, null, At(1, 0));
        _events.ListStartingBetweenAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([@event]);
        _users.ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns([_organizer, _ready, _late]);
        _late.UpdateSettings(_late.Settings with
        {
            Notifications = _late.Settings.Notifications with { EventReminderHours = 2 },
        });
        _ready.UpdateSettings(_ready.Settings with
        {
            Notifications = _ready.Settings.Notifications with { EventReminder = false },
        });

        var first = await Reminders(3, 6).RemindAboutEventsAsync(CancellationToken.None);
        var second = await Reminders(3, 6).RemindAboutEventsAsync(CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        _outbox.Received(1)
            .Add(Arg.Is<OutboxMessage>(m =>
                m.Type == EventReminderEmailRequested.Type && m.Payload.Contains(_organizer.Id.ToString(), StringComparison.Ordinal)));
    }
}
