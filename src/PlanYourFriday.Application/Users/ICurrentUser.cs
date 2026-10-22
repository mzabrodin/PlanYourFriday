namespace PlanYourFriday.Application.Users;

public interface ICurrentUser
{
    Guid UserId { get; }
}
