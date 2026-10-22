using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Calendar;

public sealed class CalendarAccess(ICalendarTokenClient oauth, ITokenProtector tokens, TimeProvider timeProvider)
{
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromMinutes(1);

    public async Task<string> GetAccessTokenAsync(
        User user,
        CalendarConnection connection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.Status == ConnectionStatus.Revoked)
        {
            throw new InvalidStateException("calendar_connection.revoked", "The calendar connection was removed.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (connection.Status == ConnectionStatus.Active && !connection.IsExpired(now + ExpiryMargin))
        {
            return tokens.Unprotect(connection.AccessToken);
        }

        try
        {
            var refreshed = await oauth.RefreshTokenAsync(tokens.Unprotect(connection.RefreshToken), cancellationToken);
            user.UpdateCalendarTokens(
                connection.Id,
                tokens.Protect(refreshed.AccessToken),
                refreshed.RefreshToken is null ? null : tokens.Protect(refreshed.RefreshToken),
                refreshed.ExpiresAt);
            return refreshed.AccessToken;
        }
        catch (CalendarAuthorizationException)
        {
            user.MarkCalendarExpired(connection.Id);
            throw new InvalidStateException(
                "calendar_connection.expired",
                "Google Calendar access has expired. Connect the calendar again.");
        }
    }
}
