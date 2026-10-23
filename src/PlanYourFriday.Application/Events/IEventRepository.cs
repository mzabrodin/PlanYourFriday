using PlanYourFriday.Domain.Events;

namespace PlanYourFriday.Application.Events;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Event?> FindConfirmedByGroupAsync(Guid groupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> ListByGroupAsync(Guid groupId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> ListNeedingSyncAsync(
        int max,
        DateTime retryFailedBefore,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> ListUpcomingForMemberAsync(Guid userId, DateTime now, CancellationToken cancellationToken);

    Task<IReadOnlyList<Event>> ListStartingBetweenAsync(DateTime from, DateTime to, CancellationToken cancellationToken);

    void Add(Event @event);
}
