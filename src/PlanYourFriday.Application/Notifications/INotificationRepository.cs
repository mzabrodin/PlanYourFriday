using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Application.Notifications;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> ListUnreadAsync(
        Guid userId,
        IReadOnlyCollection<Guid>? ids,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid userId, Guid groupId, NotificationKind kind, CancellationToken cancellationToken);

    void Add(Notification notification);
}
