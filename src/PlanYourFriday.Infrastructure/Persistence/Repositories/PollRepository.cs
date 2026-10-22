using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Polls;
using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class PollRepository(AppDbContext context) : IPollRepository
{
    public Task<Poll?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Polls().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Poll>> ListClosedUnannouncedAsync(DateTime now, CancellationToken cancellationToken)
    {
        return await Polls()
            .Where(p => !p.ClosedNotified && (p.ClosedAt != null || p.Deadline <= now))
            .ToListAsync(cancellationToken);
    }

    public void Add(Poll poll)
    {
        context.Polls.Add(poll);
    }

    public void Remove(Poll poll)
    {
        context.Polls.Remove(poll);
    }

    private IQueryable<Poll> Polls()
    {
        return context.Polls
            .Include(p => p.Participants)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .AsSplitQuery();
    }
}
