using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Application.Groups;

public static class GroupAccess
{
    public static Group EnsureMember(this Group? group, Guid userId)
    {
        if (group is null || !group.IsMember(userId))
        {
            throw new EntityNotFoundException("group.not_found", "Group not found.");
        }

        return group;
    }
}
