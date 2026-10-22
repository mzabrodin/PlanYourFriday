using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Infrastructure.Persistence;

internal sealed class OutboxWriter(AppDbContext context) : IOutbox
{
    public void Add(OutboxMessage message)
    {
        context.OutboxMessages.Add(message);
    }
}
