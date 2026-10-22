using System.Text.Json;

using Microsoft.Extensions.Options;

using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Application.Notifications;

public sealed class EventReminderEmailHandler(
    IEventRepository events,
    IUserRepository users,
    IEmailSender email,
    IOptions<AppOptions> options) : IOutboxHandler
{
    public string Type => EventReminderEmailRequested.Type;

    public async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var request = JsonSerializer.Deserialize<EventReminderEmailRequested>(payload)
            ?? throw new JsonException("Empty event reminder payload.");
        var @event = await events.GetByIdAsync(request.EventId, cancellationToken);
        var recipient = await users.GetByIdAsync(request.UserId, cancellationToken);
        if (@event is null || @event.Status != EventStatus.Confirmed || recipient is null)
        {
            return;
        }

        var link = new Uri(options.Value.PublicUrl, $"/groups/{@event.GroupId}");
        var message = NotificationEmail.ComposeReminder(@event, recipient.TimeZone, link);
        await email.SendAsync(recipient.Email, message.Subject, message.Body, cancellationToken);
    }
}
