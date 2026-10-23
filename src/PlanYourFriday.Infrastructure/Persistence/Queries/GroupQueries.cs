using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Infrastructure.Persistence.Queries;

internal sealed class GroupQueries(AppDbContext context) : IGroupQueries, ICalendarQueries
{
    private const string You = "You";

    public async Task<IReadOnlyList<GroupSummaryView>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var groups = await LoadGroupsAsync(MemberOf(userId), userId, cancellationToken);
        var users = await LoadUsersAsync(groups.SelectMany(g => g.Members).Select(m => m.UserId), cancellationToken);
        var events = await LoadConfirmedEventsAsync(groups.Select(g => g.Id), userId, cancellationToken);

        return
        [
            .. groups
                .OrderBy(g => g.WindowStart)
                .Select(g => ToSummary(g, userId, users, events.GetValueOrDefault(g.Id))),
        ];
    }

    public async Task<GroupView?> GetAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        var groups = await LoadGroupsAsync(MemberOf(userId).Where(g => g.Id == groupId), userId, cancellationToken);
        if (groups.Count == 0)
        {
            return null;
        }

        var group = groups[0];
        var isOrganizer = group.OrganizerId == userId;
        var users = await LoadUsersAsync(group.Members.Select(m => m.UserId), cancellationToken);
        var events = await LoadConfirmedEventsAsync([group.Id], userId, cancellationToken);
        var invitations = isOrganizer
            ? await context.Invitations
                .AsNoTracking()
                .Where(i => i.GroupId == groupId)
                .OrderBy(i => i.CreatedAt)
                .Select(i => new InvitationView(i.Id, i.Email, i.Status))
                .ToListAsync(cancellationToken)
            : [];
        var options = await context.TimeOptions
            .AsNoTracking()
            .Where(o => o.GroupId == groupId)
            .OrderBy(o => o.Time.Start)
            .Select(o => new
            {
                o.Id,
                o.Time.Start,
                o.Time.End,
                o.FreeUserIds,
                Votes = o.Votes.Select(v => new VoteView(v.UserId, v.Answer)).ToList(),
            })
            .ToListAsync(cancellationToken);

        return new GroupView(
            group.Id,
            group.Title,
            group.Description,
            group.OrganizerId,
            isOrganizer ? group.InviteToken : null,
            new TimeRangeView(group.WindowStart, group.WindowEnd),
            (int)group.EventDuration.TotalMinutes,
            group.AvailabilityDeadline,
            group.Status,
            group.Members.First(m => m.UserId == userId).Role,
            [
                .. Ordered(group.Members).Select(m => new MemberView(
                    m.UserId,
                    users[m.UserId].DisplayName,
                    users[m.UserId].Email,
                    users[m.UserId].PhotoUrl,
                    m.Role,
                    m.HasAvailability)),
            ],
            invitations,
            [.. options.Select(o => new TimeOptionView(o.Id, new TimeRangeView(o.Start, o.End), o.FreeUserIds, o.Votes))],
            group.VotingDeadline,
            events.GetValueOrDefault(group.Id));
    }

    public Task<MemberView?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken)
    {
        return (
                from m in context.GroupMembers.AsNoTracking()
                join u in context.Users on m.UserId equals u.Id
                where m.GroupId == groupId && m.UserId == userId
                select new MemberView(m.UserId, u.DisplayName, u.Email, u.PhotoUrl, m.Role, m.Availability.Count != 0))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EventView?> GetEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken)
    {
        var events = await LoadEventsAsync(
            context.Events.Where(e => e.Id == eventId
                && context.GroupMembers.Any(m => m.GroupId == e.GroupId && m.UserId == userId)),
            userId,
            cancellationToken);
        return events.Count == 0 ? null : events[0].View;
    }

    public async Task<IReadOnlyList<CalendarItemView>> GetAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        var groups = await LoadGroupsAsync(MemberOf(userId), userId, cancellationToken);
        var groupIds = groups.Select(g => g.Id).ToList();
        var users = await LoadUsersAsync(groups.SelectMany(g => g.Members).Select(m => m.UserId), cancellationToken);
        var events = await LoadConfirmedEventsAsync(groupIds, userId, cancellationToken);
        var mySlots = await context.GroupMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && groupIds.Contains(m.GroupId))
            .Select(m => new
            {
                m.GroupId,
                Slots = m.Availability.Select(a => new TimeRangeView(a.Slot.Start, a.Slot.End)).ToList(),
            })
            .ToDictionaryAsync(m => m.GroupId, m => m.Slots, cancellationToken);

        var items = new List<CalendarItemView>();
        foreach (var group in groups)
        {
            var people = Ordered(group.Members).Select(m => NameFor(m.UserId, userId, users)).ToList();
            var organizerName = users[group.OrganizerId].DisplayName;

            CalendarItemView Item(string id, CalendarItemKind kind, string title, TimeRangeView time, string? location)
            {
                return new CalendarItemView(
                    id, kind, group.Id, group.Title, title, time, location, people, organizerName, group.Status);
            }

            if (events.TryGetValue(group.Id, out var @event))
            {
                items.Add(Item(@event.Id.ToString(), CalendarItemKind.Event, @event.Title, @event.Time, @event.Location));
            }
            else if (group.Status is GroupStatus.Planning or GroupStatus.Voting)
            {
                items.Add(Item(
                    $"{group.Id}-window",
                    CalendarItemKind.Planning,
                    group.Title,
                    new TimeRangeView(group.WindowStart, group.WindowEnd),
                    null));
                var slots = mySlots.GetValueOrDefault(group.Id) ?? [];
                items.AddRange(slots
                    .OrderBy(s => s.Start)
                    .Select((slot, i) => Item($"{group.Id}-free-{i}", CalendarItemKind.MyFreeTime, "Free", slot, null)));
            }
        }

        return [.. items.Where(i => i.Time.Start < to && i.Time.End > from).OrderBy(i => i.Time.Start)];
    }

    internal async Task<Dictionary<Guid, EventView>> LoadConfirmedEventsAsync(
        IEnumerable<Guid> groupIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ids = groupIds.ToList();
        var events = await LoadEventsAsync(
            context.Events.Where(e => ids.Contains(e.GroupId) && e.Status == EventStatus.Confirmed),
            userId,
            cancellationToken);
        return events.ToDictionary(e => e.GroupId, e => e.View);
    }

    private IQueryable<Group> MemberOf(Guid userId)
    {
        return context.Groups.Where(g => g.Members.Any(m => m.UserId == userId));
    }

    private static GroupSummaryView ToSummary(
        GroupRow group,
        Guid userId,
        Dictionary<Guid, UserRow> users,
        EventView? @event)
    {
        var me = group.Members.First(m => m.UserId == userId);
        return new GroupSummaryView(
            group.Id,
            group.Title,
            group.Description,
            NameFor(group.OrganizerId, userId, users),
            group.Status,
            new TimeRangeView(group.WindowStart, group.WindowEnd),
            group.AvailabilityDeadline,
            group.Members.Count,
            [.. Ordered(group.Members).Select(m => NameFor(m.UserId, userId, users))],
            group.Members.Count(m => m.HasAvailability),
            me.Role,
            me.HasAvailability,
            group.MyVotePending,
            group.VotingDeadline,
            @event);
    }

    private static IEnumerable<MemberRow> Ordered(IEnumerable<MemberRow> members)
    {
        return members.OrderBy(m => m.Role == MemberRole.Organizer ? 0 : 1).ThenBy(m => m.JoinedAt);
    }

    private static string NameFor(Guid memberId, Guid userId, Dictionary<Guid, UserRow> users)
    {
        return memberId == userId ? You : users[memberId].DisplayName;
    }

    private static CalendarSyncState ToSyncState(SyncStatus? status)
    {
        return status switch
        {
            SyncStatus.Synced => CalendarSyncState.Synced,
            SyncStatus.Pending => CalendarSyncState.Pending,
            SyncStatus.Failed => CalendarSyncState.Failed,
            _ => CalendarSyncState.NotConnected,
        };
    }

    private static Task<List<GroupRow>> LoadGroupsAsync(
        IQueryable<Group> source,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return source
            .AsNoTracking()
            .Select(g => new GroupRow(
                g.Id,
                g.Title,
                g.Description,
                g.OrganizerId,
                g.InviteToken,
                g.Window.Start,
                g.Window.End,
                g.EventDuration,
                g.AvailabilityDeadline,
                g.Status,
                g.VotingDeadline,
                g.Status == GroupStatus.Voting && g.Options.Any(o => !o.Votes.Any(v => v.UserId == userId)),
                g.Members
                    .Select(m => new MemberRow(m.UserId, m.Role, m.Availability.Count != 0, m.CreatedAt))
                    .ToList()))
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, UserRow>> LoadUsersAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        return await context.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new UserRow(u.Id, u.DisplayName, u.Email, u.PhotoUrl))
            .ToDictionaryAsync(u => u.Id, cancellationToken);
    }

    private async Task<List<EventRow>> LoadEventsAsync(
        IQueryable<Event> source,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await source
            .AsNoTracking()
            .Select(e => new
            {
                e.GroupId,
                e.Id,
                e.Title,
                e.Time.Start,
                e.Time.End,
                e.Location,
                e.MeetingLink,
                e.Description,
                e.Status,
                MySync = e.CalendarLinks
                    .Where(l => context.CalendarConnections.Any(c => c.Id == l.ConnectionId && c.UserId == userId))
                    .Select(l => (SyncStatus?)l.SyncStatus)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return
        [
            .. rows.Select(e => new EventRow(
                e.GroupId,
                new EventView(
                    e.Id,
                    e.Title,
                    new TimeRangeView(e.Start, e.End),
                    e.Location ?? string.Empty,
                    e.MeetingLink,
                    e.Description,
                    e.Status,
                    ToSyncState(e.MySync)))),
        ];
    }

    private sealed record GroupRow(
        Guid Id,
        string Title,
        string? Description,
        Guid OrganizerId,
        string InviteToken,
        DateTime WindowStart,
        DateTime WindowEnd,
        TimeSpan EventDuration,
        DateTime? AvailabilityDeadline,
        GroupStatus Status,
        DateTime? VotingDeadline,
        bool MyVotePending,
        List<MemberRow> Members);

    private sealed record MemberRow(Guid UserId, MemberRole Role, bool HasAvailability, DateTime JoinedAt);

    private sealed record UserRow(Guid Id, string DisplayName, string Email, string? PhotoUrl);

    private sealed record EventRow(Guid GroupId, EventView View);
}
