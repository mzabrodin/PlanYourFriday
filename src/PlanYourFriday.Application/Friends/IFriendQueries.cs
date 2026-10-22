namespace PlanYourFriday.Application.Friends;

public interface IFriendQueries
{
    Task<FriendsOverviewView> GetOverviewAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<PersonView>> SearchAsync(Guid userId, string query, CancellationToken cancellationToken);

    Task<PersonView?> GetPersonAsync(Guid userId, Guid otherId, CancellationToken cancellationToken);
}
