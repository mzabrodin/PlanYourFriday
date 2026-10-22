using System.Text.Json;

using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

using static PlanYourFriday.UnitTests.Domain.TestTime;

namespace PlanYourFriday.UnitTests.Notifications;

public class NotificationEmailHandlerTests
{
    private readonly INotificationRepository _notifications = Substitute.For<INotificationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IGroupRepository _groups = Substitute.For<IGroupRepository>();
    private readonly IEventRepository _events = Substitute.For<IEventRepository>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly NotificationEmailHandler _handler;
    private readonly User _organizer = User.Create("org@example.com", "Max", "UTC", AuthProvider.Google, "org");
    private readonly User _friend = User.Create("friend@example.com", "Tania", "Europe/Kyiv", AuthProvider.Google, "friend");
    private readonly Group _group;

    public NotificationEmailHandlerTests()
    {
        foreach (var user in new[] { _organizer, _friend })
        {
            _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        }

        _group = Group.Create(_organizer.Id, "Board games", null, Range(3, 10, 22), TimeSpan.FromHours(2), null, At(1, 0));
        _group.AddMember(_friend.Id);
        _groups.GetByIdAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(_group);

        _handler = new NotificationEmailHandler(
            _notifications,
            _users,
            _groups,
            _events,
            _email,
            Options.Create(new AppOptions { PublicUrl = new Uri("https://pyf.example") }));
    }

    private string Request(NotificationKind kind)
    {
        var notification = Notification.ForGroup(_friend.Id, kind, _group.Id, _organizer.Id);
        _notifications.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        return JsonSerializer.Serialize(new NotificationEmailRequested(notification.Id));
    }

    [Fact]
    public async Task EventConfirmed_IsSentWithTimeInRecipientsTimeZone()
    {
        var @event = _group.ScheduleEvent("Games", null, Range(3, 18, 20), "Max's place", null, At(1, 0));
        _events.FindConfirmedByGroupAsync(_group.Id, Arg.Any<CancellationToken>()).Returns(@event);

        await _handler.HandleAsync(Request(NotificationKind.EventConfirmed), CancellationToken.None);

        await _email.Received(1)
            .SendAsync(
                "friend@example.com",
                "\"Board games\" is on!",
                Arg.Is<string>(body => body.Contains("20:00 - 22:00", StringComparison.Ordinal)
                    && body.Contains("Where: Max's place", StringComparison.Ordinal)
                    && body.Contains($"https://pyf.example/groups/{_group.Id}", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsNotSentWhenTheRecipientTurnedItOff()
    {
        _friend.UpdateSettings(_friend.Settings with
        {
            Notifications = _friend.Settings.Notifications with { EventCancelled = false },
        });

        await _handler.HandleAsync(Request(NotificationKind.EventCancelled), CancellationToken.None);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(null!, null!, null!, CancellationToken.None);
    }

    [Fact]
    public async Task IsSkippedWhenTheNotificationWasDeleted()
    {
        await _handler.HandleAsync(
            JsonSerializer.Serialize(new NotificationEmailRequested(Guid.NewGuid())), CancellationToken.None);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(null!, null!, null!, CancellationToken.None);
    }
}
