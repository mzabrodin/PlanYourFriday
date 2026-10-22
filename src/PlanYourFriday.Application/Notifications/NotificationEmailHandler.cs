using System.Text.Json;

using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Application.Notifications;

public sealed class NotificationEmailHandler(
    INotificationRepository notifications,
    IUserRepository users,
    IGroupRepository groups,
    IEventRepository events,
    IEmailSender email,
    IOptions<AppOptions> options) : IOutboxHandler
{
    public string Type => NotificationEmailRequested.Type;

    public async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<NotificationEmailRequested>(payload)
            ?? throw new JsonException("Empty notification email payload.");
        var notification = await notifications.GetByIdAsync(request.NotificationId, cancellationToken);
        if (notification?.GroupId is not { } groupId)
        {
            return;
        }

        var recipient = await users.GetByIdAsync(notification.UserId, cancellationToken);
        var group = await groups.GetByIdAsync(groupId, cancellationToken);
        if (recipient is null || group is null || !NotificationEmail.IsWanted(recipient.Settings.Notifications, notification.Kind))
        {
            return;
        }

        var actor = notification.ActorId is { } actorId ? await users.GetByIdAsync(actorId, cancellationToken) : null;
        var actorName = actor?.DisplayName ?? "Plan Your Friday";
        var details = notification.Kind == NotificationKind.EventConfirmed
            && await events.FindConfirmedByGroupAsync(group.Id, cancellationToken) is { } @event
                ? NotificationEmail.Describe(@event, recipient.TimeZone)
                : null;
        var link = new Uri(options.Value.PublicUrl, $"/groups/{group.Id}");

        var message = NotificationEmail.Compose(notification.Kind, group.Title, actorName, link, details);
        await email.SendAsync(recipient.Email, message.Subject, message.Body, cancellationToken);
    }
}
