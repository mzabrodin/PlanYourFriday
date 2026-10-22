using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Friends;
using PlanYourFriday.Domain.Friends;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class FriendshipRepository(AppDbContext context) : IFriendshipRepository
{
    public Task<Friendship?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Friendships.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public Task<Friendship?> FindBetweenAsync(Guid userId, Guid otherId, CancellationToken cancellationToken)
    {
        var (low, high) = Friendship.Order(userId, otherId);
        return context.Friendships.FirstOrDefaultAsync(f => f.UserLowId == low && f.UserHighId == high, cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> ListFriendIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var ids = await context.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserLowId == userId || f.UserHighId == userId))
            .Select(f => f.UserLowId == userId ? f.UserHighId : f.UserLowId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public void Add(Friendship friendship)
    {
        context.Friendships.Add(friendship);
    }

    public void Remove(Friendship friendship)
    {
        context.Friendships.Remove(friendship);
    }
}
