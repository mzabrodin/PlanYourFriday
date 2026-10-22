using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Polls;

public sealed class PollVote : AuditableEntity
{
    private PollVote()
    {
    }

    internal PollVote(Guid optionId, Guid userId)
    {
        OptionId = optionId;
        UserId = userId;
    }

    public Guid OptionId { get; private set; }

    public Guid UserId { get; private set; }
}
