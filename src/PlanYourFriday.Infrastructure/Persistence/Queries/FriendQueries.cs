using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Friends;
using PlanYourFriday.Domain.Friends;

namespace PlanYourFriday.Infrastructure.Persistence.Queries;

internal sealed class FriendQueries(AppDbContext context) : IFriendQueries
{
    private const int MaxSearchResults = 20;
    private const int MaxSuggestions = 12;
    private const int MaxSharedGroupsShown = 3;

    public async Task<FriendsOverviewView> GetOverviewAsync(Guid userId, CancellationToken cancellationToken)
    {
        var friendCode = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FriendCode)
            .FirstAsync(cancellationToken);
        var links = await LinksAsync(userId, null, cancellationToken);

        var friends = links
            .Where(l => l.Status == FriendshipStatus.Accepted)
            .Select(l => new FriendView(
                l.Other.Id, l.Other.DisplayName, l.Other.Username, l.Other.Email, l.Other.PhotoUrl, l.AcceptedAt ?? l.CreatedAt))
            .OrderBy(f => f.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var incoming = links
            .Where(l => l.Status == FriendshipStatus.Pending && l.RequesterId != userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new FriendRequestView(l.Id, l.ToPerson(Relation.RequestReceived), RequestDirection.Incoming, l.CreatedAt))
            .ToList();
        var outgoing = links
            .Where(l => l.Status == FriendshipStatus.Pending && l.RequesterId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new FriendRequestView(l.Id, l.ToPerson(Relation.RequestSent), RequestDirection.Outgoing, l.CreatedAt))
            .ToList();

        var known = links.Select(l => l.Other.Id).ToList();
        var suggestions = await SuggestionsAsync(userId, known, cancellationToken);

        return new FriendsOverviewView(friends, incoming, outgoing, suggestions, friendCode);
    }

    public async Task<IReadOnlyList<PersonView>> SearchAsync(Guid userId, string query, CancellationToken cancellationToken)
    {
        var people = await context.Users
            .AsNoTracking()
            .Where(u => u.Username.Contains(query) || u.DisplayName.Contains(query))
            .OrderBy(u => !u.Username.StartsWith(query))
            .ThenBy(u => u.DisplayName)
            .Take(MaxSearchResults)
            .Select(u => new Person(u.Id, u.DisplayName, u.Username, u.Email, u.PhotoUrl))
            .ToListAsync(cancellationToken);

        return await WithRelationsAsync(userId, people, cancellationToken);
    }

    public async Task<PersonView?> GetPersonAsync(Guid userId, Guid otherId, CancellationToken cancellationToken)
    {
        var person = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == otherId)
            .Select(u => new Person(u.Id, u.DisplayName, u.Username, u.Email, u.PhotoUrl))
            .FirstOrDefaultAsync(cancellationToken);

        return person is null ? null : (await WithRelationsAsync(userId, [person], cancellationToken))[0];
    }

    private async Task<List<PersonView>> WithRelationsAsync(
        Guid userId,
        IReadOnlyList<Person> people,
        CancellationToken cancellationToken)
    {
        var links = await LinksAsync(userId, [.. people.Select(p => p.Id)], cancellationToken);
        return
        [
            .. people.Select(person =>
            {
                if (person.Id == userId)
                {
                    return person.ToView(Relation.Self);
                }

                var link = links.FirstOrDefault(l => l.Other.Id == person.Id);
                return person.ToView(link?.RelationFor(userId) ?? Relation.None);
            }),
        ];
    }

    private async Task<List<FriendshipLink>> LinksAsync(
        Guid userId,
        IReadOnlyCollection<Guid>? otherIds,
        CancellationToken cancellationToken)
    {
        var users = context.Users.AsQueryable();
        if (otherIds is not null)
        {
            var selected = otherIds.ToList();
            users = users.Where(u => selected.Contains(u.Id));
        }

        return await (
                from f in context.Friendships.AsNoTracking()
                where f.UserLowId == userId || f.UserHighId == userId
                join u in users on (f.UserLowId == userId ? f.UserHighId : f.UserLowId) equals u.Id
                select new FriendshipLink(
                    f.Id,
                    f.Status,
                    f.RequesterId,
                    f.CreatedAt,
                    f.AcceptedAt,
                    new Person(u.Id, u.DisplayName, u.Username, u.Email, u.PhotoUrl)))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<FriendSuggestionView>> SuggestionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> excluded,
        CancellationToken cancellationToken)
    {
        var excludedIds = excluded.Append(userId).ToList();
        var shared = await (
                from mine in context.GroupMembers
                where mine.UserId == userId
                join g in context.Groups on mine.GroupId equals g.Id
                join other in context.GroupMembers on g.Id equals other.GroupId
                where !excludedIds.Contains(other.UserId)
                join u in context.Users on other.UserId equals u.Id
                select new
                {
                    Person = new Person(u.Id, u.DisplayName, u.Username, u.Email, u.PhotoUrl),
                    g.Title,
                    g.CreatedAt,
                })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return
        [
            .. shared
                .GroupBy(s => s.Person.Id)
                .OrderByDescending(g => g.Count())
                .ThenByDescending(g => g.Max(s => s.CreatedAt))
                .Take(MaxSuggestions)
                .Select(g => new FriendSuggestionView(
                    g.First().Person.ToView(Relation.None),
                    [.. g.OrderByDescending(s => s.CreatedAt).Select(s => s.Title).Distinct().Take(MaxSharedGroupsShown)])),
        ];
    }

    private sealed record Person(Guid Id, string DisplayName, string Username, string Email, string? PhotoUrl)
    {
        public PersonView ToView(Relation relation)
        {
            return new PersonView(Id, DisplayName, Username, PhotoUrl, relation);
        }
    }

    private sealed record FriendshipLink(
        Guid Id,
        FriendshipStatus Status,
        Guid RequesterId,
        DateTime CreatedAt,
        DateTime? AcceptedAt,
        Person Other)
    {
        public PersonView ToPerson(Relation relation)
        {
            return Other.ToView(relation);
        }

        public Relation RelationFor(Guid userId)
        {
            if (Status == FriendshipStatus.Accepted)
            {
                return Relation.Friend;
            }

            return RequesterId == userId ? Relation.RequestSent : Relation.RequestReceived;
        }
    }
}
