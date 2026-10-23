using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Events;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class EventRepository(AppDbContext context) : IEventRepository
{
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Events
            .Include(e => e.CalendarLinks)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public Task<Event?> FindConfirmedByGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return context.Events
            .Include(e => e.CalendarLinks)
            .FirstOrDefaultAsync(e => e.GroupId == groupId && e.Status == EventStatus.Confirmed, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        return await context.Events
            .Include(e => e.CalendarLinks)
            .Where(e => e.GroupId == groupId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> ListNeedingSyncAsync(
        int max,
        DateTime retryFailedBefore,
        CancellationToken cancellationToken)
    {
        var activeConnections = context.CalendarConnections
            .Where(c => c.Status == ConnectionStatus.Active)
            .Select(c => c.Id);

        return await context.Events
            .Include(e => e.CalendarLinks)
            .Where(e => e.CalendarLinks.Any(l =>
                activeConnections.Contains(l.ConnectionId)
                && (l.SyncStatus == SyncStatus.Pending
                    || (l.SyncStatus == SyncStatus.Failed && l.UpdatedAt < retryFailedBefore))))
            .OrderBy(e => e.UpdatedAt)
            .Take(max)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> ListUpcomingForMemberAsync(
        Guid userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await context.Events
            .Include(e => e.CalendarLinks)
            .Where(e => e.Status == EventStatus.Confirmed
                && e.Time.End > now
                && context.GroupMembers.Any(m => m.GroupId == e.GroupId && m.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> ListStartingBetweenAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        return await context.Events
            .Where(e => e.Status == EventStatus.Confirmed && e.Time.Start > from && e.Time.Start <= to)
            .ToListAsync(cancellationToken);
    }

    public void Add(Event @event)
    {
        context.Events.Add(@event);
    }
}
