using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed class TimeOption : Entity
{
    private readonly List<OptionVote> _votes = [];

    private TimeOption()
    {
    }

    internal TimeOption(Guid groupId, TimeRange time, IEnumerable<Guid> freeUserIds)
    {
        GroupId = groupId;
        Time = time;
        FreeUserIds = [.. freeUserIds];
    }

    public Guid GroupId { get; private set; }

    public TimeRange Time { get; private set; } = null!;

    public Guid[] FreeUserIds { get; private set; } = [];

    public IReadOnlyCollection<OptionVote> Votes => _votes;

    internal void SetVote(Guid userId, VoteAnswer answer)
    {
        var existing = _votes.FirstOrDefault(v => v.UserId == userId);
        if (existing is null)
        {
            _votes.Add(new OptionVote(Id, userId, answer));
        }
        else
        {
            existing.Change(answer);
        }
    }

    internal void RemoveVote(Guid userId)
    {
        _votes.RemoveAll(v => v.UserId == userId);
    }
}
