using PlanYourFriday.Application.Events;

namespace PlanYourFriday.Application.Groups;

public interface IGroupQueries
{
    Task<IReadOnlyList<GroupSummaryView>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<GroupView?> GetAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);

    Task<MemberView?> GetMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);

    Task<EventView?> GetEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken);
}
