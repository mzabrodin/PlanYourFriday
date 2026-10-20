using System.Text.Json;

using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

using PlanYourFriday.Api.ErrorHandling;
using PlanYourFriday.Infrastructure;
using PlanYourFriday.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddControllers(options => options.ModelMetadataDetailsProviders.Add(
        new SystemTextJsonValidationMetadataProvider(JsonNamingPolicy.SnakeCaseLower)))
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddApiProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await app.Services.MigrateDatabaseAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapHealthChecks("/api/health");
app.MapControllers();

app.Run();
