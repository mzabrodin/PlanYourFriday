namespace PlanYourFriday.Application.Outbox;

public interface IOutboxHandler
{
    string Type { get; }

    Task HandleAsync(string payload, CancellationToken cancellationToken);
}
