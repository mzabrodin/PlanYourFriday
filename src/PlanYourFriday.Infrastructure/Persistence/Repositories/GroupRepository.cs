using Microsoft.EntityFrameworkCore;

using PlanYourFriday.Application.Groups;
using PlanYourFriday.Domain.Groups;

namespace PlanYourFriday.Infrastructure.Persistence.Repositories;

internal sealed class GroupRepository(AppDbContext context) : IGroupRepository
{
    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Groups().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public Task<Group?> FindByInviteTokenAsync(string inviteToken, CancellationToken cancellationToken)
    {
        return Groups().FirstOrDefaultAsync(g => g.InviteToken == inviteToken, cancellationToken);
    }

    public Task<Group?> FindByInvitationIdAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        return Groups().FirstOrDefaultAsync(g => g.Invitations.Any(i => i.Id == invitationId), cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListOrganizedByAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await Groups().Where(g => g.OrganizerId == userId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Group>> ListWithDeadlineBetweenAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken)
    {
        return await Groups()
            .Where(g => g.Status == GroupStatus.Planning
                && g.AvailabilityDeadline != null
                && g.AvailabilityDeadline > from
                && g.AvailabilityDeadline <= to)
            .ToListAsync(cancellationToken);
    }

    public void Add(Group group)
    {
        context.Groups.Add(group);
    }

    public void Remove(Group group)
    {
        context.Groups.Remove(group);
    }

    private IQueryable<Group> Groups()
    {
        return context.Groups
            .Include(g => g.Members)
            .ThenInclude(m => m.Availability)
            .Include(g => g.Invitations)
            .Include(g => g.Options)
            .ThenInclude(o => o.Votes)
            .AsSplitQuery();
    }
}
