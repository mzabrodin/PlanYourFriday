using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Application.Polls;

public interface IPollRepository
{
    Task<Poll?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Poll>> ListClosedUnannouncedAsync(DateTime now, CancellationToken cancellationToken);

    void Add(Poll poll);

    void Remove(Poll poll);
}
