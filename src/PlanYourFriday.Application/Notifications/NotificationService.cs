using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Users;

namespace PlanYourFriday.Application.Notifications;

public sealed class NotificationService(
    ICurrentUser currentUser,
    INotificationRepository notifications,
    INotificationQueries queries,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<IReadOnlyList<NotificationView>> ListAsync(CancellationToken cancellationToken)
    {
        return queries.ListAsync(currentUser.UserId, cancellationToken);
    }

    public async Task MarkReadAsync(IReadOnlyCollection<Guid>? ids, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var notification in await notifications.ListUnreadAsync(currentUser.UserId, ids, cancellationToken))
        {
            notification.MarkRead(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
