namespace PlanYourFriday.Application.Friends;

public enum Relation
{
    Self,
    None,
    Friend,
    RequestSent,
    RequestReceived,
}

public enum RequestDirection
{
    Incoming,
    Outgoing,
}

public sealed record PersonView(Guid UserId, string DisplayName, string Username, Relation Relation);

public sealed record FriendView(Guid UserId, string DisplayName, string Username, string Email, DateTime Since);

public sealed record FriendRequestView(Guid Id, PersonView Person, RequestDirection Direction, DateTime CreatedAt);

public sealed record FriendSuggestionView(PersonView Person, IReadOnlyList<string> SharedGroups);

public sealed record FriendsOverviewView(
    IReadOnlyList<FriendView> Friends,
    IReadOnlyList<FriendRequestView> Incoming,
    IReadOnlyList<FriendRequestView> Outgoing,
    IReadOnlyList<FriendSuggestionView> Suggestions,
    string FriendCode);

public sealed record FriendCodeView(string FriendCode);
