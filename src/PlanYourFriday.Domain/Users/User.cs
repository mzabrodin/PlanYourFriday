using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Users;

public sealed class User : Entity
{
    private readonly List<UserIdentity> _identities = [];
    private readonly List<CalendarConnection> _calendarConnections = [];

    private User()
    {
    }

    private User(string email, string displayName, string timeZone, AuthProvider provider, string subject)
    {
        Email = email;
        DisplayName = displayName;
        TimeZone = timeZone;
        _identities.Add(new UserIdentity(Id, provider, subject));
    }

    public string Email { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;

    public string TimeZone { get; private set; } = null!;

    public IReadOnlyCollection<UserIdentity> Identities => _identities;

    public IReadOnlyCollection<CalendarConnection> CalendarConnections => _calendarConnections;

    public static User Create(string email, string displayName, string timeZone, AuthProvider provider, string subject)
    {
        return new User(email, displayName, timeZone, provider, subject);
    }

    public void UpdateProfile(string displayName, string timeZone)
    {
        DisplayName = displayName;
        TimeZone = timeZone;
    }

    public UserIdentity AddIdentity(AuthProvider provider, string subject)
    {
        var existing = _identities.FirstOrDefault(i => i.Provider == provider && i.Subject == subject);
        if (existing is not null)
        {
            return existing;
        }

        var identity = new UserIdentity(Id, provider, subject);
        _identities.Add(identity);
        return identity;
    }

    public CalendarConnection ConnectCalendar(
        CalendarProvider provider,
        string externalAccountId,
        string calendarId,
        string accessToken,
        string refreshToken,
        DateTime tokenExpiresAt,
        IEnumerable<string> scopes)
    {
        var connection = _calendarConnections.FirstOrDefault(c =>
            c.Provider == provider && c.ExternalAccountId == externalAccountId);
        var hasOtherPrimary = _calendarConnections.Any(c => c.IsPrimary && c != connection);

        if (connection is null)
        {
            connection = new CalendarConnection(
                Id,
                provider,
                externalAccountId,
                calendarId,
                accessToken,
                refreshToken,
                tokenExpiresAt,
                scopes,
                isPrimary: !hasOtherPrimary);
            _calendarConnections.Add(connection);
            return connection;
        }

        connection.Reconnect(calendarId, accessToken, refreshToken, tokenExpiresAt, scopes);
        if (!hasOtherPrimary)
        {
            connection.MarkPrimary();
        }

        return connection;
    }

    public void DisconnectCalendar(Guid connectionId)
    {
        var connection = FindConnection(connectionId);

        var wasPrimary = connection.IsPrimary;
        connection.Revoke();

        if (wasPrimary)
        {
            _calendarConnections
                .FirstOrDefault(c => c != connection && c.Status == ConnectionStatus.Active)
                ?.MarkPrimary();
        }
    }

    public void UpdateCalendarTokens(Guid connectionId, string accessToken, string? refreshToken, DateTime tokenExpiresAt)
    {
        FindConnection(connectionId).UpdateTokens(accessToken, refreshToken, tokenExpiresAt);
    }

    public void MarkCalendarExpired(Guid connectionId)
    {
        FindConnection(connectionId).MarkExpired();
    }

    private CalendarConnection FindConnection(Guid connectionId)
    {
        return _calendarConnections.FirstOrDefault(c => c.Id == connectionId)
            ?? throw new EntityNotFoundException("calendar_connection.not_found", "Calendar connection not found.");
    }
}
