using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Domain.Polls;

public sealed class PollParticipant : AuditableEntity
{
    private PollParticipant()
    {
    }

    internal PollParticipant(Guid pollId, Guid userId)
    {
        PollId = pollId;
        UserId = userId;
    }

    public Guid PollId { get; private set; }

    public Guid UserId { get; private set; }
}
