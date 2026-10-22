using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Domain.Notifications;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository(AppDbContext context) : INotificationRepository
{
    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Notifications.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListUnreadAsync(
        Guid userId,
        IReadOnlyCollection<Guid>? ids,
        CancellationToken cancellationToken)
    {
        var query = context.Notifications.Where(n => n.UserId == userId && n.ReadAt == null);
        if (ids is not null)
        {
            var selected = ids.ToList();
            query = query.Where(n => selected.Contains(n.Id));
        }

        return await query.ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid userId, Guid groupId, NotificationKind kind, CancellationToken cancellationToken)
    {
        return context.Notifications.AnyAsync(
            n => n.UserId == userId && n.GroupId == groupId && n.Kind == kind,
            cancellationToken);
    }

    public void Add(Notification notification)
    {
        context.Notifications.Add(notification);
    }
}
