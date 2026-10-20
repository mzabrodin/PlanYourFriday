using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Application.Outbox;

public sealed class OutboxMessage : Entity
{
    private OutboxMessage()
    {
    }

    private OutboxMessage(string type, string payload)
    {
        Type = type;
        Payload = payload;
    }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public int Attempts { get; private set; }

    public DateTime? NextAttemptAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public static OutboxMessage Create(string type, string payload)
    {
        return new OutboxMessage(type, payload);
    }

    public void MarkProcessed(DateTime now)
    {
        Attempts++;
        ProcessedAt = now;
        NextAttemptAt = null;
    }

    public void ScheduleRetry(DateTime at)
    {
        Attempts++;
        NextAttemptAt = at;
    }
}
