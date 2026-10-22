using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;

using Npgsql;

using NSubstitute.ClearExtensions;

using PlanYourFriday.Application.Calendar;
using PlanYourFriday.Application.Common;

using Testcontainers.PostgreSql;

namespace PlanYourFriday.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string[] KeptTables = ["__EFMigrationsHistory", "data_protection_keys"];

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public IExternalCalendarClient Calendar { get; } = Substitute.For<IExternalCalendarClient>();

    public ICalendarTokenClient CalendarTokens { get; } = Substitute.For<ICalendarTokenClient>();

    public IEmailSender Email { get; } = Substitute.For<IEmailSender>();

    public FakeTimeProvider Clock { get; } = new(DateTimeOffset.UtcNow) { AutoAdvanceAmount = TimeSpan.FromMilliseconds(1) };

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        _ = Server;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        Calendar.ClearSubstitute(ClearOptions.All);
        CalendarTokens.ClearSubstitute(ClearOptions.All);
        Email.ClearSubstitute(ClearOptions.All);

        await using var connection = new NpgsqlConnection(_database.GetConnectionString());
        await connection.OpenAsync();

        var tables = new List<string>();
        await using (var list = new NpgsqlCommand(
            "SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND NOT tablename = ANY(@kept)", connection))
        {
            list.Parameters.AddWithValue("kept", KeptTables);
            await using var reader = await list.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

#pragma warning disable CA2100
        await using var truncate = new NpgsqlCommand(
            $"TRUNCATE {string.Join(", ", tables.Select(t => $"\"{t}\""))} CASCADE", connection);
#pragma warning restore CA2100
        await truncate.ExecuteNonQueryAsync();
    }

    public HttpClient CreateBrowser()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Authentication:Google:ClientId", string.Empty);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(Calendar);
            services.AddSingleton(CalendarTokens);
            services.AddSingleton(Email);
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
