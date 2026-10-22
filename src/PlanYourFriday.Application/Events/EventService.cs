using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Events;
using PlanYourFriday.Domain.Groups;
using PlanYourFriday.Domain.Notifications;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Events;

public sealed class EventService(
    ICurrentUser currentUser,
    IGroupRepository groups,
    IEventRepository events,
    IUserRepository users,
    IGroupQueries queries,
    NotificationPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<EventView> ConfirmAsync(
        Guid groupId,
        EventDetails details,
        TimeRange time,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);

        group.EnsureOrganizer(currentUser.UserId);
        var @event = group.ScheduleEvent(
            details.Title,
            details.Description,
            time,
            details.Location,
            details.MeetingLink,
            timeProvider.GetUtcNow().UtcDateTime);

        var members = await users.ListByIdsAsync([.. group.Members.Select(m => m.UserId)], cancellationToken);
        foreach (var calendar in members.Select(m => m.FindPrimaryCalendar()).OfType<CalendarConnection>())
        {
            if (calendar.Status == ConnectionStatus.Active)
            {
                @event.AddCalendarLink(calendar.Id);
            }
        }

        events.Add(@event);
        publisher.NotifyMembers(group, NotificationKind.EventConfirmed, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetViewAsync(@event.Id, cancellationToken);
    }

    public async Task<EventView> UpdateAsync(Guid eventId, EventDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var (group, @event) = await GetAsOrganizerAsync(eventId, cancellationToken);
        @event.Update(details.Title, details.Description, details.Location, details.MeetingLink);
        publisher.NotifyMembers(group, NotificationKind.EventUpdated, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetViewAsync(@event.Id, cancellationToken);
    }

    public async Task CancelAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var (group, @event) = await GetAsOrganizerAsync(eventId, cancellationToken);
        group.CancelEvent(@event);
        publisher.NotifyMembers(group, NotificationKind.EventCancelled, currentUser.UserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<(Group Group, Event Event)> GetAsOrganizerAsync(Guid eventId, CancellationToken cancellationToken)
    {
        var @event = await events.GetByIdAsync(eventId, cancellationToken);
        var group = @event is null ? null : await groups.GetByIdAsync(@event.GroupId, cancellationToken);
        if (@event is null || group is null || !group.IsMember(currentUser.UserId))
        {
            throw new EntityNotFoundException("event.not_found", "Event not found.");
        }

        group.EnsureOrganizer(currentUser.UserId);
        return (group, @event);
    }

    private async Task<EventView> GetViewAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await queries.GetEventAsync(eventId, currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("The event was not found after saving.");
    }
}
