using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed record UserView(
    Guid Id,
    string Email,
    string Username,
    string DisplayName,
    string? PhotoUrl,
    string TimeZone,
    bool CalendarConnected)
{
    public static UserView From(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserView(
            user.Id,
            user.Email,
            user.Username,
            user.DisplayName,
            user.PhotoUrl,
            user.TimeZone,
            user.CalendarConnections.Any(c => c.Status == ConnectionStatus.Active));
    }
}
