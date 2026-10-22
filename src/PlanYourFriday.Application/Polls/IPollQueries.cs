namespace PlanYourFriday.Application.Polls;

public interface IPollQueries
{
    Task<IReadOnlyList<PollSummaryView>> ListAsync(Guid userId, DateTime now, CancellationToken cancellationToken);

    Task<PollView?> GetAsync(Guid pollId, Guid userId, DateTime now, CancellationToken cancellationToken);
}
