using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Polls;

public sealed class Poll : Entity
{
    public const int MaxOptions = 12;
    public const int MaxTitleLength = 120;
    public const int MaxOptionLength = 80;

    private readonly List<PollParticipant> _participants = [];
    private readonly List<PollOption> _options = [];

    private Poll()
    {
    }

    private Poll(
        Guid creatorId,
        string title,
        string? description,
        DateTime? deadline,
        bool multipleChoice,
        bool allowNewOptions)
    {
        CreatorId = creatorId;
        Title = title;
        Description = description;
        Deadline = deadline;
        MultipleChoice = multipleChoice;
        AllowNewOptions = allowNewOptions;
    }

    public Guid CreatorId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? Description { get; private set; }

    public DateTime? Deadline { get; private set; }

    public bool MultipleChoice { get; private set; }

    public bool AllowNewOptions { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public bool ClosedNotified { get; private set; }

    public Guid? GroupId { get; private set; }

    public IReadOnlyCollection<PollParticipant> Participants => _participants;

    public IReadOnlyCollection<PollOption> Options => _options;

    public static Poll Create(
        Guid creatorId,
        string title,
        string? description,
        IEnumerable<Guid> participantIds,
        IEnumerable<string> options,
        DateTime? deadline,
        bool multipleChoice,
        bool allowNewOptions,
        DateTime now)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length is 0 or > MaxTitleLength)
        {
            throw new BusinessRuleViolationException("poll.invalid_title", $"Title must be 1-{MaxTitleLength} characters.");
        }

        if (deadline <= now)
        {
            throw new BusinessRuleViolationException("poll.deadline_in_past", "The deadline must be in the future.");
        }

        var poll = new Poll(creatorId, trimmedTitle, string.IsNullOrWhiteSpace(description) ? null : description.Trim(), deadline, multipleChoice, allowNewOptions);

        poll._participants.Add(new PollParticipant(poll.Id, creatorId));
        foreach (var userId in participantIds.Distinct().Where(id => id != creatorId))
        {
            poll._participants.Add(new PollParticipant(poll.Id, userId));
        }

        if (poll._participants.Count < 2)
        {
            throw new BusinessRuleViolationException("poll.no_participants", "Invite at least one friend.");
        }

        foreach (var option in options)
        {
            poll.AddOptionCore(option, creatorId);
        }

        return poll._options.Count < 2 ? throw new BusinessRuleViolationException("poll.too_few_options", "Add at least two different options.") : poll;
    }

    public bool IsClosed(DateTime now)
    {
        return ClosedAt is not null || (Deadline is not null && Deadline <= now);
    }

    public bool IsParticipant(Guid userId)
    {
        return _participants.Any(p => p.UserId == userId);
    }

    public bool HasVoted(Guid userId)
    {
        return _options.Any(o => o.HasVoteFrom(userId));
    }

    public PollOption AddOption(Guid userId, string title, DateTime now)
    {
        EnsureParticipant(userId);
        EnsureOpen(now);

        if (!AllowNewOptions && userId != CreatorId)
        {
            throw new AccessDeniedException("poll.options_locked", "Only the creator can add options to this poll.");
        }

        return AddOptionCore(title, userId)
            ?? throw new AlreadyExistsException("poll.option_exists", "This option is already in the poll.");
    }

    public void Vote(Guid userId, IReadOnlyCollection<Guid> optionIds, DateTime now)
    {
        EnsureParticipant(userId);
        EnsureOpen(now);

        var chosen = optionIds.Distinct().ToList();
        if (!MultipleChoice && chosen.Count > 1)
        {
            throw new BusinessRuleViolationException("poll.single_choice", "This poll allows only one choice.");
        }

        if (chosen.Any(id => _options.All(o => o.Id != id)))
        {
            throw new EntityNotFoundException("poll_option.not_found", "Option not found.");
        }

        foreach (var option in _options)
        {
            if (chosen.Contains(option.Id))
            {
                option.AddVote(userId);
            }
            else
            {
                option.RemoveVote(userId);
            }
        }
    }

    public void Close(Guid userId, DateTime now)
    {
        if (userId != CreatorId)
        {
            throw new AccessDeniedException("poll.not_creator", "Only the creator can close the poll.");
        }

        if (IsClosed(now))
        {
            return;
        }

        ClosedAt = now;
    }

    public void MarkClosedNotified()
    {
        ClosedNotified = true;
    }

    public IReadOnlyList<PollOption> Winners(DateTime now)
    {
        if (!IsClosed(now))
        {
            return [];
        }

        var top = _options.Count == 0 ? 0 : _options.Max(o => o.Votes.Count);
        return top == 0 ? [] : [.. _options.Where(o => o.Votes.Count == top)];
    }

    public void LinkGroup(Guid userId, Guid groupId, DateTime now)
    {
        EnsureParticipant(userId);

        if (!IsClosed(now))
        {
            throw new InvalidStateException("poll.still_open", "Close the poll before planning the winner.");
        }

        if (GroupId is not null && GroupId != groupId)
        {
            throw new InvalidStateException("poll.already_planned", "An event was already planned from this poll.");
        }

        GroupId = groupId;
    }

    public void EnsureCreator(Guid userId)
    {
        if (userId != CreatorId)
        {
            throw new AccessDeniedException("poll.not_creator", "Only the creator can do this.");
        }
    }

    private PollOption? AddOptionCore(string title, Guid userId)
    {
        var trimmed = title.Trim();
        switch (trimmed.Length)
        {
            case 0:
                return null;
            case > MaxOptionLength:
                throw new BusinessRuleViolationException("poll.option_too_long", $"Options must be at most {MaxOptionLength} characters.");
        }

        if (_options.Any(o => string.Equals(o.Title, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        if (_options.Count >= MaxOptions)
        {
            throw new BusinessRuleViolationException("poll.too_many_options", $"A poll can have at most {MaxOptions} options.");
        }

        var option = new PollOption(Id, trimmed, userId, _options.Count);
        _options.Add(option);
        return option;
    }

    private void EnsureParticipant(Guid userId)
    {
        if (!IsParticipant(userId))
        {
            throw new AccessDeniedException("poll.not_a_participant", "Only participants can do this.");
        }
    }

    private void EnsureOpen(DateTime now)
    {
        if (IsClosed(now))
        {
            throw new InvalidStateException("poll.closed", "This poll is closed.");
        }
    }
}
