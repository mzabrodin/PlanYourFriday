using PlanYourFriday.Domain.Friends;

namespace PlanYourFriday.Application.Friends;

public interface IFriendshipRepository
{
    Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Friendship?> FindBetweenAsync(Guid userId, Guid otherId, CancellationToken cancellationToken);

    Task<IReadOnlySet<Guid>> ListFriendIdsAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Friendship friendship);

    void Remove(Friendship friendship);
}
