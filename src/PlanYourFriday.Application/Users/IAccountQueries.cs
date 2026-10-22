namespace PlanYourFriday.Application.Users;

public interface IAccountQueries
{
    Task<AccountExport?> ExportAsync(Guid userId, CancellationToken cancellationToken);
}
