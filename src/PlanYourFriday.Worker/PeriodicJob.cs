namespace PlanYourFriday.Worker;

internal abstract partial class PeriodicJob(IServiceScopeFactory scopeFactory, ILogger logger, TimeSpan interval)
    : BackgroundService
{
    protected abstract string Name { get; }

    protected abstract Task<int> RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processed = await RunOnceAsync(scope.ServiceProvider, stoppingToken);
                if (processed > 0)
                {
                    LogProcessed(logger, Name, processed);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(logger, exception, Name);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Job} processed {Count} item(s)")]
    private static partial void LogProcessed(ILogger logger, string job, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Job} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, string job);
}
