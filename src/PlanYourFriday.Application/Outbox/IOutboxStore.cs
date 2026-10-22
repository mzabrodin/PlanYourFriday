namespace PlanYourFriday.Application.Outbox;

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> ListDueAsync(int max, DateTime now, CancellationToken cancellationToken);
}
