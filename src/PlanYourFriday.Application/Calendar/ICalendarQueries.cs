namespace PlanYourFriday.Application.Calendar;

public interface ICalendarQueries
{
    Task<IReadOnlyList<CalendarItemView>> GetAsync(
        Guid userId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
