using PlanYourFriday.Application.Common;
using PlanYourFriday.Application.Events;
using PlanYourFriday.Application.Groups;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;
using PlanYourFriday.Domain.Users;

namespace PlanYourFriday.Application.Calendar;

public sealed class CalendarConnectionService(
    ICurrentUser currentUser,
    AccountService account,
    IUserRepository users,
    IGroupRepository groups,
    IEventRepository events,
    IExternalCalendarClient client,
    ICalendarTokenClient oauth,
    CalendarAccess access,
    ITokenProtector tokens,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const string PrimaryCalendarId = "primary";

    public async Task ConnectGoogleAsync(Guid userId, GoogleCalendarGrant grant, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grant);

        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new UnauthenticatedException("auth.user_not_found", "The signed-in user no longer exists.");
        var existing = user.CalendarConnections.FirstOrDefault(c =>
            c.Provider == CalendarProvider.Google && c.ExternalAccountId == grant.Subject);
        var refreshToken = grant.RefreshToken is not null
            ? tokens.Protect(grant.RefreshToken)
            : existing?.RefreshToken
                ?? throw new BusinessRuleViolationException(
                    "calendar_connection.no_offline_access",
                    "Google did not grant offline access to the calendar. Try connecting again.");

        var connection = user.ConnectCalendar(
            CalendarProvider.Google,
            grant.Subject,
            grant.Email,
            PrimaryCalendarId,
            tokens.Protect(grant.AccessToken),
            refreshToken,
            grant.ExpiresAt,
            grant.Scopes);

        if (connection.IsPrimary)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var @event in await events.ListUpcomingForMemberAsync(userId, now, cancellationToken))
            {
                @event.AddCalendarLink(connection.Id);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CalendarConnectionView>> ListAsync(CancellationToken cancellationToken)
    {
        var user = await account.GetAsync(cancellationToken);
        var views = new List<CalendarConnectionView>();
        foreach (var connection in user.CalendarConnections.Where(c => c.Status != ConnectionStatus.Revoked))
        {
            views.Add(CalendarConnectionView.From(connection, await TryListCalendarsAsync(user, connection, cancellationToken)));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return views;
    }

    public async Task<CalendarConnectionView> ChooseCalendarsAsync(
        Guid connectionId,
        IReadOnlyCollection<string> busyCalendarIds,
        string writeCalendarId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(busyCalendarIds);

        var user = await account.GetAsync(cancellationToken);
        var connection = user.CalendarConnections.FirstOrDefault(c => c.Id == connectionId && c.Status != ConnectionStatus.Revoked)
            ?? throw new EntityNotFoundException("calendar_connection.not_found", "Calendar connection not found.");

        var token = await access.GetAccessTokenAsync(user, connection, cancellationToken);
        var calendars = await client.ListCalendarsAsync(token, cancellationToken);
        var known = calendars.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);
        if (!known.Contains(writeCalendarId) || busyCalendarIds.Any(id => !known.Contains(id)))
        {
            throw new BusinessRuleViolationException("calendar_connection.unknown_calendar", "Unknown calendar.");
        }

        user.ChooseCalendars(connection.Id, busyCalendarIds, writeCalendarId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return CalendarConnectionView.From(connection, calendars);
    }

    public async Task DisconnectPrimaryAsync(CancellationToken cancellationToken)
    {
        var user = await account.GetAsync(cancellationToken);
        var connection = user.FindPrimaryCalendar()
            ?? throw new EntityNotFoundException("calendar_connection.not_found", "No calendar is connected.");

        try
        {
            await oauth.RevokeAsync(tokens.Unprotect(connection.RefreshToken), cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or CalendarAuthorizationException)
        {
        }

        user.DisconnectCalendar(connection.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ImportAvailabilityAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var user = await account.GetAsync(cancellationToken);
        var group = (await groups.GetByIdAsync(groupId, cancellationToken)).EnsureMember(currentUser.UserId);
        var connection = user.FindPrimaryCalendar()
            ?? throw new InvalidStateException("calendar_connection.not_connected", "Connect Google Calendar first.");

        var token = await access.GetAccessTokenAsync(user, connection, cancellationToken);
        var busy = await client.GetBusyAsync(token, connection.BusyCalendarIds, group.Window, cancellationToken);
        var usual = FreeTime.UsualSlots(group.Window, user.Settings.UsualFreeTime, FindTimeZone(user.TimeZone));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        group.SetAvailability(user.Id, FreeTime.Subtract(usual, busy), now);
        user.MarkCalendarSynced(connection.Id, now);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ExternalCalendar>> TryListCalendarsAsync(
        User user,
        CalendarConnection connection,
        CancellationToken cancellationToken)
    {
        try
        {
            var token = await access.GetAccessTokenAsync(user, connection, cancellationToken);
            return await client.ListCalendarsAsync(token, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or DomainException or CalendarAuthorizationException)
        {
            return [];
        }
    }

    private static TimeZoneInfo FindTimeZone(string id)
    {
        return TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone) ? timeZone : TimeZoneInfo.Utc;
    }
}
