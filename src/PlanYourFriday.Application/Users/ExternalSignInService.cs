using PlanYourFriday.Application.Common;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed class ExternalSignInService(IUserRepository users, IUnitOfWork unitOfWork)
{
    public const string DefaultTimeZone = "UTC";

    public async Task<User> SignInAsync(ExternalLogin login, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(login);

        var user = await users.FindByIdentityAsync(login.Provider, login.Subject, cancellationToken);
        if (user is null)
        {
            user = await users.FindByEmailAsync(login.Email, cancellationToken);
            if (user is not null)
            {
                if (!login.EmailVerified)
                {
                    throw new AlreadyExistsException(
                        "user.email_taken", "An account with this email already exists. Sign in with it first.");
                }

                user.AddIdentity(login.Provider, login.Subject);
            }
            else
            {
                user = User.Create(login.Email, login.DisplayName, ValidTimeZone(login.TimeZone), login.Provider, login.Subject);
                user.ChangeUsername(await FreeUsernameAsync(user.Username, cancellationToken));
                users.Add(user);
            }
        }

        if (login.PhotoUrl is not null)
        {
            user.SetPhotoUrl(login.PhotoUrl);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static string ValidTimeZone(string? timeZone)
    {
        return !string.IsNullOrWhiteSpace(timeZone) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out _)
            ? timeZone
            : DefaultTimeZone;
    }

    private async Task<string> FreeUsernameAsync(string wanted, CancellationToken cancellationToken)
    {
        var taken = await users.ListUsernamesStartingWithAsync(wanted, cancellationToken);
        if (!taken.Contains(wanted))
        {
            return wanted;
        }

        var suffix = 2;
        while (taken.Contains($"{wanted}{suffix}"))
        {
            suffix++;
        }

        return $"{wanted}{suffix}";
    }
}
