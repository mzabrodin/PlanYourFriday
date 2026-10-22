using PlanYourFriday.Application.Users;
using PlanYourFriday.Domain.Common;

namespace PlanYourFriday.Application.Calendar;

public sealed class CalendarService(ICurrentUser currentUser, ICalendarQueries queries)
{
    public Task<IReadOnlyList<CalendarItemView>> GetAsync(TimeRange range, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(range);
        return queries.GetAsync(currentUser.UserId, range.Start, range.End, cancellationToken);
    }
}
