using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Polls;

namespace PlanYourFriday.Infrastructure.Persistence.Queries;

internal sealed class PollQueries(AppDbContext context) : IPollQueries
{
    private const string UnknownName = "Deleted user";

    public async Task<IReadOnlyList<PollSummaryView>> ListAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var rows = await LoadAsync(userId, null, cancellationToken);
        var names = await NamesAsync(rows, cancellationToken);

        return
        [
            .. rows
                .OrderBy(r => r.IsClosed(now))
                .ThenByDescending(r => r.CreatedAt)
                .Select(r => new PollSummaryView(
                    r.Id,
                    r.Title,
                    names.GetValueOrDefault(r.CreatorId)?.DisplayName ?? UnknownName,
                    r.IsClosed(now) ? PollStatus.Closed : PollStatus.Open,
                    r.Deadline,
                    r.ParticipantIds.Count,
                    r.ParticipantIds.Count(r.HasVoted),
                    r.Options.Count,
                    !r.IsClosed(now) && !r.HasVoted(userId),
                    [.. r.Leading().Select(o => o.Title)],
                    r.GroupId,
                    r.CreatedAt)),
        ];
    }

    public async Task<PollView?> GetAsync(Guid pollId, Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        var row = (await LoadAsync(userId, pollId, cancellationToken)).SingleOrDefault();
        if (row is null)
        {
            return null;
        }

        var names = await NamesAsync([row], cancellationToken);
        string Name(Guid id)
        {
            return names.GetValueOrDefault(id)?.DisplayName ?? UnknownName;
        }

        return new PollView(
            row.Id,
            row.Title,
            row.Description,
            row.CreatorId,
            Name(row.CreatorId),
            row.IsClosed(now) ? PollStatus.Closed : PollStatus.Open,
            row.Deadline,
            row.MultipleChoice,
            row.AllowNewOptions,
            [
                .. row.ParticipantIds
                    .OrderBy(id => id == row.CreatorId ? 0 : 1)
                    .ThenBy(Name, StringComparer.CurrentCultureIgnoreCase)
                    .Select(id => new PollPersonView(id, Name(id), names.GetValueOrDefault(id)?.PhotoUrl)),
            ],
            [.. row.Options.Select(o => new PollOptionView(o.Id, o.Title, o.AddedById, o.VoterIds))],
            row.IsClosed(now) ? [.. row.Leading().Select(o => o.Id)] : [],
            row.GroupId,
            row.CreatedAt);
    }

    private async Task<List<PollRow>> LoadAsync(Guid userId, Guid? pollId, CancellationToken cancellationToken)
    {
        var polls = context.Polls.AsNoTracking().Where(p => p.Participants.Any(x => x.UserId == userId));
        if (pollId is not null)
        {
            polls = polls.Where(p => p.Id == pollId);
        }

        return await polls
            .Select(p => new PollRow(
                p.Id,
                p.Title,
                p.Description,
                p.CreatorId,
                p.Deadline,
                p.ClosedAt,
                p.MultipleChoice,
                p.AllowNewOptions,
                p.GroupId,
                p.CreatedAt,
                p.Participants.Select(x => x.UserId).ToList(),
                p.Options
                    .OrderBy(o => o.Position)
                    .Select(o => new OptionRow(o.Id, o.Title, o.AddedById, o.Votes.Select(v => v.UserId).ToList()))
                    .ToList()))
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, PersonRow>> NamesAsync(IEnumerable<PollRow> rows, CancellationToken cancellationToken)
    {
        var ids = rows.SelectMany(r => r.ParticipantIds.Append(r.CreatorId)).Distinct().ToList();
        return await context.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new PersonRow(u.DisplayName, u.PhotoUrl), cancellationToken);
    }

    private sealed record PersonRow(string DisplayName, string? PhotoUrl);

    private sealed record OptionRow(Guid Id, string Title, Guid AddedById, List<Guid> VoterIds);

    private sealed record PollRow(
        Guid Id,
        string Title,
        string? Description,
        Guid CreatorId,
        DateTime? Deadline,
        DateTime? ClosedAt,
        bool MultipleChoice,
        bool AllowNewOptions,
        Guid? GroupId,
        DateTime CreatedAt,
        List<Guid> ParticipantIds,
        List<OptionRow> Options)
    {
        public bool IsClosed(DateTime now)
        {
            return ClosedAt is not null || Deadline <= now;
        }

        public bool HasVoted(Guid userId)
        {
            return Options.Any(o => o.VoterIds.Contains(userId));
        }

        public IEnumerable<OptionRow> Leading()
        {
            var top = Options.Count == 0 ? 0 : Options.Max(o => o.VoterIds.Count);
            return top == 0 ? [] : Options.Where(o => o.VoterIds.Count == top);
        }
    }
}
