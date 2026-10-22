using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Notifications;
using PlanYourFriday.Application.Outbox;
using PlanYourFriday.Application.Polls;

namespace PlanYourFriday.Worker;

internal sealed class OutboxJob(IServiceScopeFactory scopeFactory, ILogger<OutboxJob> logger)
    : PeriodicJob(scopeFactory, logger, TimeSpan.FromSeconds(5))
{
    protected override string Name => "Outbox";

    protected override Task<int> RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<OutboxDispatcher>().DispatchDueAsync(cancellationToken);
    }
}

internal sealed class CalendarSyncJob(IServiceScopeFactory scopeFactory, ILogger<CalendarSyncJob> logger)
    : PeriodicJob(scopeFactory, logger, TimeSpan.FromSeconds(15))
{
    protected override string Name => "Calendar sync";

    protected override Task<int> RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        return services.GetRequiredService<CalendarSyncService>().SyncPendingAsync(cancellationToken);
    }
}

internal sealed class ReminderJob(IServiceScopeFactory scopeFactory, ILogger<ReminderJob> logger)
    : PeriodicJob(scopeFactory, logger, TimeSpan.FromMinutes(1))
{
    protected override string Name => "Reminders";

    protected override async Task<int> RunOnceAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var reminders = services.GetRequiredService<ReminderService>();
        var polls = services.GetRequiredService<PollService>();
        return await reminders.RemindAboutDeadlinesAsync(cancellationToken)
            + await reminders.RemindAboutEventsAsync(cancellationToken)
            + await polls.AnnounceClosedAsync(cancellationToken);
    }
}
