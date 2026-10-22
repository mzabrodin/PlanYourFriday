namespace PlanYourFriday.Application.Notifications;

public interface INotificationQueries
{
    Task<IReadOnlyList<NotificationView>> ListAsync(Guid userId, CancellationToken cancellationToken);
}
