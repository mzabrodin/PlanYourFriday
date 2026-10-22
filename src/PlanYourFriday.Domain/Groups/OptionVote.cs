using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Groups;

public sealed class OptionVote : AuditableEntity
{
    private OptionVote()
    {
    }

    internal OptionVote(Guid optionId, Guid userId, VoteAnswer answer)
    {
        OptionId = optionId;
        UserId = userId;
        Answer = answer;
    }

    public Guid OptionId { get; private set; }

    public Guid UserId { get; private set; }

    public VoteAnswer Answer { get; private set; }

    internal void Change(VoteAnswer answer)
    {
        Answer = answer;
    }
}
