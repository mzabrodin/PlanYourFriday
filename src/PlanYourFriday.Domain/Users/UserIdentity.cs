using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Users;

public sealed class UserIdentity : Entity
{
    private UserIdentity()
    {
    }

    internal UserIdentity(Guid userId, AuthProvider provider, string subject)
    {
        UserId = userId;
        Provider = provider;
        Subject = subject;
    }

    public Guid UserId { get; private set; }

    public AuthProvider Provider { get; private set; }

    public string Subject { get; private set; } = null!;
}
