using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Polls;

namespace PlanYourFriday.Application.Polls;

public static class PollAccess
{
    public static Poll EnsureParticipant(this Poll? poll, Guid userId)
    {
        if (poll is null || !poll.IsParticipant(userId))
        {
            throw new EntityNotFoundException("poll.not_found", "Poll not found.");
        }

        return poll;
    }
}
