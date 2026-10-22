using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Application.Friends;

public static class FriendAccess
{
    public static async Task EnsureFriendsAsync(
        this IFriendshipRepository friendships,
        Guid userId,
        IEnumerable<Guid> otherIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(friendships);

        var others = otherIds.Where(id => id != userId).ToList();
        if (others.Count == 0)
        {
            return;
        }

        var friends = await friendships.ListFriendIdsAsync(userId, cancellationToken);
        if (others.Any(id => !friends.Contains(id)))
        {
            throw new AccessDeniedException("friendship.not_friends", "You can only add people from your friends list.");
        }
    }
}
