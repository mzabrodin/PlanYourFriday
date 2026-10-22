using PlanYourFriday.Application;
using PlanYourFriday.Application.Users;
using PlanYourFriday.Infrastructure;
using PlanYourFriday.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSingleton<ICurrentUser, NoCurrentUser>();
builder.Services.AddHostedService<OutboxJob>();
builder.Services.AddHostedService<CalendarSyncJob>();
builder.Services.AddHostedService<ReminderJob>();

var host = builder.Build();
host.Run();
