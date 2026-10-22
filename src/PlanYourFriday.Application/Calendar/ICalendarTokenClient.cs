namespace PlanYourFriday.Application.Calendar;

public interface ICalendarTokenClient
{
    Task<RefreshedToken> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string token, CancellationToken cancellationToken);
}

public sealed record RefreshedToken(string AccessToken, string? RefreshToken, DateTime ExpiresAt);

public sealed class CalendarAuthorizationException(string message) : Exception(message);
