using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Application.Groups;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Group?> FindByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken);

    Task<Group?> FindByInvitationIdAsync(Guid invitationId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Group>> ListOrganizedByAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Group>> ListWithDeadlineBetweenAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);

    void Add(Group group);

    void Remove(Group group);
}
