using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Friends;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Users;

namespace PlanYourFriday.Infrastructure.Persistence.Queries;

internal sealed class AccountQueries(
    AppDbContext context,
    GroupQueries groupQueries,
    IFriendQueries friendQueries,
    TimeProvider timeProvider) : IAccountQueries
{
    public async Task<AccountExport?> ExportAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Identities)
            .Include(u => u.CalendarConnections)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var memberships = await (
                from m in context.GroupMembers.AsNoTracking()
                join g in context.Groups on m.GroupId equals g.Id
                where m.UserId == userId
                orderby g.Window.Start
                select new
                {
                    g.Id,
                    g.Title,
                    m.Role,
                    g.Window.Start,
                    g.Window.End,
                    g.Status,
                    Slots = m.Availability.Select(a => new TimeRangeView(a.Slot.Start, a.Slot.End)).ToList(),
                })
            .ToListAsync(cancellationToken);

        var events = await groupQueries.LoadConfirmedEventsAsync(memberships.Select(m => m.Id), userId, cancellationToken);

        var votes = await (
                from o in context.TimeOptions.AsNoTracking()
                from v in o.Votes
                where v.UserId == userId
                select new { o.GroupId, o.Time.Start, o.Time.End, v.Answer })
            .ToListAsync(cancellationToken);

        var friends = await friendQueries.GetOverviewAsync(userId, cancellationToken);

        var polls = await context.Polls
            .AsNoTracking()
            .Where(p => p.Participants.Any(x => x.UserId == userId))
            .OrderBy(p => p.CreatedAt)
            .Select(p => new ExportedPoll(
                p.Id,
                p.Title,
                p.CreatorId == userId,
                p.Deadline,
                p.ClosedAt,
                p.Options.OrderBy(o => o.Position).Select(o => o.Title).ToList(),
                p.Options.Where(o => o.Votes.Any(v => v.UserId == userId)).Select(o => o.Title).ToList()))
            .ToListAsync(cancellationToken);

        var notifications = await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new ExportedNotification(n.Kind, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(cancellationToken);

        return new AccountExport(
            timeProvider.GetUtcNow().UtcDateTime,
            new ExportedProfile(
                user.Id,
                user.Email,
                user.Username,
                user.DisplayName,
                user.PhotoUrl,
                user.TimeZone,
                user.FriendCode,
                user.CreatedAt,
                [.. user.Identities.Select(i => i.Provider).Distinct()]),
            UserSettingsView.From(user.Settings),
            [
                .. user.CalendarConnections.Select(c => new ExportedCalendarConnection(
                    c.Provider, c.AccountEmail, c.Status, c.CreatedAt)),
            ],
            [
                .. memberships.Select(m => new ExportedGroup(
                    m.Id,
                    m.Title,
                    m.Role,
                    new TimeRangeView(m.Start, m.End),
                    m.Status,
                    events.GetValueOrDefault(m.Id),
                    [.. m.Slots.OrderBy(s => s.Start)],
                    [
                        .. votes
                            .Where(v => v.GroupId == m.Id)
                            .OrderBy(v => v.Start)
                            .Select(v => new ExportedTimeVote(new TimeRangeView(v.Start, v.End), v.Answer)),
                    ])),
            ],
            [.. friends.Friends.Select(f => new ExportedFriend(f.DisplayName, f.Username, f.Since))],
            [
                .. friends.Incoming.Concat(friends.Outgoing)
                    .Select(r => new ExportedPendingFriendship(
                        r.Person.DisplayName, r.Person.Username, r.Direction, r.CreatedAt)),
            ],
            polls,
            notifications);
    }
}
