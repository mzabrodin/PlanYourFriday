# Build from the repository root: docker build -f src/PlanYourFriday.Worker/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore in its own layer so it is cached until a project file changes
COPY global.json Directory.Build.props .editorconfig ./
COPY src/PlanYourFriday.Domain/PlanYourFriday.Domain.csproj src/PlanYourFriday.Domain/
COPY src/PlanYourFriday.Application/PlanYourFriday.Application.csproj src/PlanYourFriday.Application/
COPY src/PlanYourFriday.Infrastructure/PlanYourFriday.Infrastructure.csproj src/PlanYourFriday.Infrastructure/
COPY src/PlanYourFriday.Worker/PlanYourFriday.Worker.csproj src/PlanYourFriday.Worker/
RUN dotnet restore src/PlanYourFriday.Worker/PlanYourFriday.Worker.csproj

COPY src/ src/
RUN dotnet publish src/PlanYourFriday.Worker/PlanYourFriday.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

USER app

ENTRYPOINT ["dotnet", "PlanYourFriday.Worker.dll"]
