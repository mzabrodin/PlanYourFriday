using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Notifications;

namespace PlanYourFriday.Infrastructure.Persistence.Queries;

internal sealed class NotificationQueries(AppDbContext context) : INotificationQueries
{
    private const int Limit = 100;

    public async Task<IReadOnlyList<NotificationView>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
                from n in context.Notifications.AsNoTracking()
                join g in context.Groups on n.GroupId equals (Guid?)g.Id into groups
                from groupItem in groups.DefaultIfEmpty()
                join p in context.Polls on n.PollId equals (Guid?)p.Id into polls
                from poll in polls.DefaultIfEmpty()
                join a in context.Users on n.ActorId equals (Guid?)a.Id into actors
                from actor in actors.DefaultIfEmpty()
                where n.UserId == userId
                orderby n.CreatedAt descending
                select new NotificationView(
                    n.Id,
                    n.Kind,
                    n.GroupId,
                    groupItem == null ? null : groupItem.Title,
                    n.PollId,
                    poll == null ? null : poll.Title,
                    actor == null ? null : actor.DisplayName,
                    n.CreatedAt,
                    n.ReadAt != null))
            .Take(Limit)
            .ToListAsync(cancellationToken);
    }
}
