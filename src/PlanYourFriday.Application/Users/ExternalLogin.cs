using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Users;

public sealed record ExternalLogin(
    AuthProvider Provider,
    string Subject,
    string Email,
    bool EmailVerified,
    string DisplayName,
    string? PhotoUrl,
    string? TimeZone
);
