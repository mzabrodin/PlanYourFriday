using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Friends;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Friends;

public sealed class FriendService(
    ICurrentUser currentUser,
    IUserRepository users,
    IFriendshipRepository friendships,
    IFriendQueries queries,
    NotificationPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const int MinimumQueryLength = 2;

    public Task<FriendsOverviewView> GetOverviewAsync(CancellationToken cancellationToken)
    {
        return queries.GetOverviewAsync(currentUser.UserId, cancellationToken);
    }

    public async Task<IReadOnlyList<PersonView>> SearchAsync(string? query, CancellationToken cancellationToken)
    {
        var normalized = (query ?? string.Empty).Trim().TrimStart('@').ToLowerInvariant();
        return normalized.Length < MinimumQueryLength
            ? []
            : await queries.SearchAsync(currentUser.UserId, normalized, cancellationToken);
    }

    public async Task<PersonView> GetByFriendCodeAsync(string code, CancellationToken cancellationToken)
    {
        var other = await FindByFriendCodeAsync(code, cancellationToken);
        return await GetPersonAsync(other.Id, cancellationToken);
    }

    public async Task<PersonView> AddByFriendCodeAsync(string code, CancellationToken cancellationToken)
    {
        var other = await FindByFriendCodeAsync(code, cancellationToken);
        var me = currentUser.UserId;
        if (other.Id == me)
        {
            throw new BusinessRuleViolationException("friendship.self", "This is your own friend link.");
        }

        var friendship = await friendships.FindBetweenAsync(me, other.Id, cancellationToken);
        if (friendship is null)
        {
            friendships.Add(Friendship.Befriend(me, other.Id, Now()));
            publisher.NotifyFriend(other.Id, NotificationKind.FriendAccepted, me);
        }
        else if (!friendship.IsAccepted)
        {
            friendship.AcceptByFriendCode(Now());
            publisher.NotifyFriend(other.Id, NotificationKind.FriendAccepted, me);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetPersonAsync(other.Id, cancellationToken);
    }

    public async Task<PersonView> SendRequestAsync(Guid otherId, CancellationToken cancellationToken)
    {
        var me = currentUser.UserId;
        if (await users.GetByIdAsync(otherId, cancellationToken) is null)
        {
            throw new EntityNotFoundException("user.not_found", "User not found.");
        }

        var friendship = await friendships.FindBetweenAsync(me, otherId, cancellationToken);
        if (friendship is null)
        {
            friendships.Add(Friendship.Request(me, otherId));
            publisher.NotifyFriend(otherId, NotificationKind.FriendRequest, me);
        }
        else if (!friendship.IsAccepted && friendship.AddresseeId == me)
        {
            friendship.Accept(me, Now());
            publisher.NotifyFriend(otherId, NotificationKind.FriendAccepted, me);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetPersonAsync(otherId, cancellationToken);
    }

    public async Task AcceptAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var friendship = await GetOwnRequestAsync(requestId, cancellationToken);
        if (friendship.IsAccepted)
        {
            return;
        }

        friendship.Accept(currentUser.UserId, Now());
        publisher.NotifyFriend(friendship.RequesterId, NotificationKind.FriendAccepted, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var friendship = await GetOwnRequestAsync(requestId, cancellationToken);
        if (friendship.IsAccepted)
        {
            throw new InvalidStateException("friendship.already_friends", "You are already friends.");
        }

        friendships.Remove(friendship);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFriendAsync(Guid friendId, CancellationToken cancellationToken)
    {
        var friendship = await friendships.FindBetweenAsync(currentUser.UserId, friendId, cancellationToken);
        if (friendship is not null)
        {
            friendships.Remove(friendship);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<User> FindByFriendCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await users.FindByFriendCodeAsync(code.Trim().ToUpperInvariant(), cancellationToken)
            ?? throw new EntityNotFoundException("friend_code.not_found", "This friend link is invalid or was replaced.");
    }

    private async Task<Friendship> GetOwnRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var friendship = await friendships.GetByIdAsync(requestId, cancellationToken);
        return friendship is not null && friendship.Involves(currentUser.UserId)
            ? friendship
            : throw new EntityNotFoundException("friend_request.not_found", "Friend request not found.");
    }

    private async Task<PersonView> GetPersonAsync(Guid otherId, CancellationToken cancellationToken)
    {
        return await queries.GetPersonAsync(currentUser.UserId, otherId, cancellationToken)
            ?? throw new EntityNotFoundException("user.not_found", "User not found.");
    }

    private DateTime Now()
    {
        return timeProvider.GetUtcNow().UtcDateTime;
    }
}
