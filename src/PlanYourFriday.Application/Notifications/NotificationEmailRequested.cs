using System.Text.Json;

using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Application.Notifications;

public sealed record NotificationEmailRequested(Guid NotificationId)
{
    public const string Type = "notification.email_requested";

    public OutboxMessage ToOutboxMessage()
    {
        return OutboxMessage.Create(Type, JsonSerializer.Serialize(this));
    }
}
