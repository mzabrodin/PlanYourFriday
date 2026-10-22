using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Outbox;

namespace PlanYourFriday.Infrastructure.Persistence;

internal sealed class OutboxStore(AppDbContext context) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> ListDueAsync(int max, DateTime now, CancellationToken cancellationToken)
    {
        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(max)
            .ToListAsync(cancellationToken);
    }
}
