using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Worker;

internal sealed class NoCurrentUser : ICurrentUser
{
    public Guid UserId => throw new UnauthenticatedException("auth.not_signed_in", "The worker has no signed-in user.");
}
