using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Users().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> FindByIdentityAsync(AuthProvider provider, string subject, CancellationToken cancellationToken)
    {
        return Users()
            .FirstOrDefaultAsync(
                u => u.Identities.Any(i => i.Provider == provider && i.Subject == subject),
                cancellationToken);
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return Users().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> FindByFriendCodeAsync(string friendCode, CancellationToken cancellationToken)
    {
        return Users().FirstOrDefaultAsync(u => u.FriendCode == friendCode, cancellationToken);
    }

    public Task<bool> IsUsernameTakenAsync(string username, Guid exceptUserId, CancellationToken cancellationToken)
    {
        return context.Users.AnyAsync(u => u.Username == username && u.Id != exceptUserId, cancellationToken);
    }

    public async Task<IReadOnlySet<string>> ListUsernamesStartingWithAsync(string prefix, CancellationToken cancellationToken)
    {
        var usernames = await context.Users
            .Where(u => u.Username.StartsWith(prefix))
            .Select(u => u.Username)
            .ToListAsync(cancellationToken);
        return usernames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public Task<User?> FindByCalendarConnectionAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        return Users().FirstOrDefaultAsync(u => u.CalendarConnections.Any(c => c.Id == connectionId), cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        var selected = ids.ToList();
        return await Users().Where(u => selected.Contains(u.Id)).ToListAsync(cancellationToken);
    }

    public void Add(User user)
    {
        context.Users.Add(user);
    }

    public void Remove(User user)
    {
        context.Users.Remove(user);
    }

    private IQueryable<User> Users()
    {
        return context.Users
            .Include(u => u.Identities)
            .Include(u => u.CalendarConnections)
            .AsSplitQuery();
    }
}
