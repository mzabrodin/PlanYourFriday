using System.Text.Json;

using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Application.Groups;

public sealed record InvitationEmailRequested(Guid GroupId, Guid InvitationId)
{
    public const string Type = "invitation.email_requested";

    public OutboxMessage ToOutboxMessage()
    {
        return OutboxMessage.Create(Type, JsonSerializer.Serialize(this));
    }
}
