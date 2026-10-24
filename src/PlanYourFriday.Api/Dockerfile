# Build from the repository root: docker build -f src/PlanYourFriday.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Restore in its own layer so it is cached until a project file changes
COPY global.json Directory.Build.props .editorconfig ./
COPY src/PlanYourFriday.Domain/PlanYourFriday.Domain.csproj src/PlanYourFriday.Domain/
COPY src/PlanYourFriday.Application/PlanYourFriday.Application.csproj src/PlanYourFriday.Application/
COPY src/PlanYourFriday.Infrastructure/PlanYourFriday.Infrastructure.csproj src/PlanYourFriday.Infrastructure/
COPY src/PlanYourFriday.Api/PlanYourFriday.Api.csproj src/PlanYourFriday.Api/
RUN dotnet restore src/PlanYourFriday.Api/PlanYourFriday.Api.csproj

COPY src/ src/
RUN dotnet publish src/PlanYourFriday.Api/PlanYourFriday.Api.csproj -c Release -o /app --no-restore

# EF migration bundle for the one-off migrate container (docker compose builds it with target: migrate)
FROM build AS migrations-build
COPY dotnet-tools.json ./
RUN dotnet tool restore
# Building the bundle needs some connection string; at run time it reads the real one from ConnectionStrings__Database
RUN ConnectionStrings__Database="Host=unused" dotnet ef migrations bundle \
    --project src/PlanYourFriday.Infrastructure \
    --startup-project src/PlanYourFriday.Api \
    --configuration Release \
    --output /efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrate
WORKDIR /app
COPY --from=migrations-build /efbundle .
USER app
ENTRYPOINT ["./efbundle"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl is only for the Docker Compose health check
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app .

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlanYourFriday.Api.dll"]
