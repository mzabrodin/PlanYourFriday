using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed class AccountService(
    ICurrentUser currentUser,
    IUserRepository users,
    IGroupRepository groups,
    IEventRepository events,
    IAccountQueries queries,
    IOutbox outbox,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<User> GetAsync(CancellationToken cancellationToken)
    {
        return await users.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthenticatedException("auth.user_not_found", "The signed-in user no longer exists.");
    }

    public async Task<UserView> GetProfileAsync(CancellationToken cancellationToken)
    {
        return UserView.From(await GetAsync(cancellationToken));
    }

    public async Task<UserSettingsView> GetSettingsAsync(CancellationToken cancellationToken)
    {
        return UserSettingsView.From((await GetAsync(cancellationToken)).Settings);
    }

    public async Task<UserView> UpdateProfileAsync(
        string displayName,
        string username,
        string timeZone,
        CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        user.UpdateProfile(displayName, timeZone);

        var previous = user.Username;
        user.ChangeUsername(username);
        if (user.Username != previous && await users.IsUsernameTakenAsync(user.Username, user.Id, cancellationToken))
        {
            throw new AlreadyExistsException("user.username_taken", $"@{user.Username} is already taken.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UserView.From(user);
    }

    public async Task<FriendCodeView> RegenerateFriendCodeAsync(CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        user.RegenerateFriendCode();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new FriendCodeView(user.FriendCode);
    }

    public async Task<UserSettingsView> UpdateSettingsAsync(UserSettings settings, CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        user.UpdateSettings(settings);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UserSettingsView.From(user.Settings);
    }

    public async Task<AccountExport> ExportAsync(CancellationToken cancellationToken)
    {
        return await queries.ExportAsync(currentUser.UserId, cancellationToken)
            ?? throw new UnauthenticatedException("auth.user_not_found", "The signed-in user no longer exists.");
    }

    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var group in await groups.ListOrganizedByAsync(user.Id, cancellationToken))
        {
            // The group's events go with it, so take their copies out of the members' calendars first.
            CalendarEventRemovalRequested.QueueFor(outbox, await events.ListByGroupAsync(group.Id, cancellationToken), now);
            groups.Remove(group);
        }

        users.Remove(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task EndAllSessionsAsync(CancellationToken cancellationToken)
    {
        var user = await GetAsync(cancellationToken);
        user.EndAllSessions();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
