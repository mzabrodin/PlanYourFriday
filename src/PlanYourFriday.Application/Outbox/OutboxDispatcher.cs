using Microsoft.Extensions.Logging;

using PlanYourFriday.Application.Common;

namespace PlanYourFriday.Application.Outbox;

public sealed partial class OutboxDispatcher(
    IOutboxStore store,
    IEnumerable<IOutboxHandler> handlers,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<OutboxDispatcher> logger)
{
    private const int BatchSize = 20;
    private const int MaxAttempts = 8;

    public async Task<int> DispatchDueAsync(CancellationToken cancellationToken)
    {
        var messages = await store.ListDueAsync(BatchSize, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        foreach (var message in messages)
        {
            await DispatchAsync(message, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return messages.Count;
    }

    private async Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var handler = handlers.FirstOrDefault(h => h.Type == message.Type);
        if (handler is null)
        {
            LogUnknownType(logger, message.Id, message.Type);
            message.MarkProcessed(now);
            return;
        }

        try
        {
            await handler.HandleAsync(message.Payload, cancellationToken);
            message.MarkProcessed(now);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (message.Attempts + 1 >= MaxAttempts)
            {
                LogGaveUp(logger, exception, message.Id, message.Type);
                message.MarkProcessed(now);
            }
            else
            {
                LogRetry(logger, exception, message.Id, message.Type);
                message.ScheduleRetry(now + TimeSpan.FromMinutes(Math.Pow(2, message.Attempts)));
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {MessageId} has unknown type {Type}")]
    private static partial void LogUnknownType(ILogger logger, Guid messageId, string type);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} ({Type}) failed, will retry")]
    private static partial void LogRetry(ILogger logger, Exception exception, Guid messageId, string type);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {MessageId} ({Type}) failed too many times, giving up")]
    private static partial void LogGaveUp(ILogger logger, Exception exception, Guid messageId, string type);
}
