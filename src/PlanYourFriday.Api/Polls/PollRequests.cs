using System.ComponentModel.DataAnnotations;

using PlanYourFriday.Application.Polls;
using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Api.Polls;

public sealed record CreatePollRequest(
    [Required]
    [MaxLength(Poll.MaxTitleLength)]
    string Title,
    [MaxLength(2000)] string? Description,
    [Required]
    [Length(1, 50)]
    IReadOnlyList<Guid> ParticipantIds,
    [Required]
    [MaxLength(Poll.MaxOptions)]
    IReadOnlyList<string> Options,
    DateTimeOffset? Deadline,
    bool MultipleChoice,
    bool AllowNewOptions)
{
    public PollDraft ToDraft()
    {
        return new PollDraft(
            Title,
            Description,
            ParticipantIds,
            Options,
            Deadline?.UtcDateTime,
            MultipleChoice,
            AllowNewOptions);
    }
}

public sealed record AddPollOptionRequest(
    [Required]
    [MaxLength(Poll.MaxOptionLength)]
    string Title);

public sealed record VotePollRequest(
    [Required]
    [MaxLength(Poll.MaxOptions)]
    IReadOnlyList<Guid> OptionIds);
