using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Users;

public sealed class CalendarConnection : Entity
{
    private CalendarConnection()
    {
    }

    internal CalendarConnection(
        Guid userId,
        CalendarProvider provider,
        string externalAccountId,
        string calendarId,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiresAt,
        IEnumerable<string> scopes,
        bool isPrimary)
    {
        UserId = userId;
        Provider = provider;
        ExternalAccountId = externalAccountId;
        IsPrimary = isPrimary;
        Reconnect(calendarId, accessToken, refreshToken, tokenExpiresAt, scopes);
    }

    public Guid UserId { get; private set; }

    public CalendarProvider Provider { get; private set; }

    public string ExternalAccountId { get; private set; } = null!;

    public string CalendarId { get; private set; } = null!;

    public string AccessToken { get; private set; } = null!;

    public string RefreshToken { get; private set; } = null!;

    public DateTime TokenExpiresAt { get; private set; }

    public string[] Scopes { get; private set; } = [];

    public ConnectionStatus Status { get; private set; }

    public bool IsPrimary { get; private set; }

    public bool IsExpired(DateTime now)
    {
        return TokenExpiresAt <= now;
    }

    internal void UpdateTokens(string accessToken, string? refreshToken, DateTime tokenExpiresAt)
    {
        if (Status == ConnectionStatus.Revoked)
        {
            throw new InvalidStateException("calendar_connection.revoked", "Cannot update tokens of a revoked calendar connection.");
        }

        AccessToken = accessToken;
        RefreshToken = string.IsNullOrEmpty(refreshToken) ? RefreshToken : refreshToken;
        TokenExpiresAt = tokenExpiresAt;
        Status = ConnectionStatus.Active;
    }

    internal void MarkExpired()
    {
        if (Status == ConnectionStatus.Active)
        {
            Status = ConnectionStatus.Expired;
        }
    }

    internal void Revoke()
    {
        Status = ConnectionStatus.Revoked;
        IsPrimary = false;
    }

    internal void Reconnect(
        string calendarId,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiresAt,
        IEnumerable<string> scopes)
    {
        CalendarId = calendarId;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        TokenExpiresAt = tokenExpiresAt;
        Scopes = [.. scopes.Distinct(StringComparer.Ordinal)];
        Status = ConnectionStatus.Active;
    }

    internal void MarkPrimary()
    {
        IsPrimary = true;
    }
}
