namespace PlanYourFriday.Application.Outbox;

public interface IOutbox
{
    void Add(OutboxMessage message);
}
