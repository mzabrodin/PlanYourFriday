using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByIdentityAsync(AuthProvider provider, string subject, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> FindByFriendCodeAsync(string friendCode, CancellationToken cancellationToken);

    Task<User?> FindByCalendarConnectionAsync(Guid connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> ListByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<bool> IsUsernameTakenAsync(string username, Guid exceptUserId, CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> ListUsernamesStartingWithAsync(string prefix, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}
