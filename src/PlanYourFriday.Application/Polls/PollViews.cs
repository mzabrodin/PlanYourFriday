namespace PlanYourFriday.Application.Polls;

public enum PollStatus
{
    Open,
    Closed,
}

public sealed record PollPersonView(Guid UserId, string DisplayName);

public sealed record PollOptionView(Guid Id, string Title, Guid AddedById, IReadOnlyList<Guid> VoterIds);

public sealed record PollView(
    Guid Id,
    string Title,
    string? Description,
    Guid CreatorId,
    string CreatorName,
    PollStatus Status,
    DateTime? Deadline,
    bool MultipleChoice,
    bool AllowNewOptions,
    IReadOnlyList<PollPersonView> Participants,
    IReadOnlyList<PollOptionView> Options,
    IReadOnlyList<Guid> WinnerOptionIds,
    Guid? GroupId,
    DateTime CreatedAt);

public sealed record PollSummaryView(
    Guid Id,
    string Title,
    string CreatorName,
    PollStatus Status,
    DateTime? Deadline,
    int ParticipantCount,
    int VotedCount,
    int OptionCount,
    bool MyVotePending,
    IReadOnlyList<string> LeadingOptions,
    Guid? GroupId,
    DateTime CreatedAt);

public sealed record PollDraft(
    string Title,
    string? Description,
    IReadOnlyCollection<Guid> ParticipantIds,
    IReadOnlyCollection<string> Options,
    DateTime? Deadline,
    bool MultipleChoice,
    bool AllowNewOptions);
