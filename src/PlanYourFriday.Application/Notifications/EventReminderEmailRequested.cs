using System.Text.Json;

using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Application.Notifications;

public sealed record EventReminderEmailRequested(Guid EventId, Guid UserId)
{
    public const string Type = "event.reminder_email_requested";

    public OutboxMessage ToOutboxMessage()
    {
        return OutboxMessage.Create(Type, JsonSerializer.Serialize(this));
    }
}
