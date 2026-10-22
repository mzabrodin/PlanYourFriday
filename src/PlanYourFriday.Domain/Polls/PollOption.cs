using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Polls;

public sealed class PollOption : Entity
{
    private readonly List<PollVote> _votes = [];

    private PollOption()
    {
    }

    internal PollOption(Guid pollId, string title, Guid addedById, int position)
    {
        PollId = pollId;
        Title = title;
        AddedById = addedById;
        Position = position;
    }

    public Guid PollId { get; private set; }

    public string Title { get; private set; } = null!;

    public Guid AddedById { get; private set; }

    public int Position { get; private set; }

    public IReadOnlyCollection<PollVote> Votes => _votes;

    internal bool HasVoteFrom(Guid userId)
    {
        return _votes.Any(v => v.UserId == userId);
    }

    internal void AddVote(Guid userId)
    {
        if (!HasVoteFrom(userId))
        {
            _votes.Add(new PollVote(Id, userId));
        }
    }

    internal void RemoveVote(Guid userId)
    {
        _votes.RemoveAll(v => v.UserId == userId);
    }
}
