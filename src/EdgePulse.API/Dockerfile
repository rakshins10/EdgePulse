# EdgePulse.API — multi-stage .NET 9 image
#
# Build CONTEXT must be the repo root (not this folder), because this project
# references sibling projects (Domain, Application, Infrastructure). The CI
# workflow sets `context: .` accordingly.
#
#   docker build -f src/EdgePulse.API/Dockerfile -t edgepulse-api .

# ── Build stage: the full .NET SDK (compiler + tooling) ──────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project files first and restore. Docker caches this layer, so
# dependencies are re-downloaded only when a .csproj changes — not on every
# source edit. This makes rebuilds much faster.
COPY src/EdgePulse.Domain/EdgePulse.Domain.csproj                 EdgePulse.Domain/
COPY src/EdgePulse.Application/EdgePulse.Application.csproj         EdgePulse.Application/
COPY src/EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj  EdgePulse.Infrastructure/
COPY src/EdgePulse.API/EdgePulse.API.csproj                        EdgePulse.API/
RUN dotnet restore EdgePulse.API/EdgePulse.API.csproj

# Now copy the rest of the source and publish a Release build.
COPY src/EdgePulse.Domain/         EdgePulse.Domain/
COPY src/EdgePulse.Application/     EdgePulse.Application/
COPY src/EdgePulse.Infrastructure/ EdgePulse.Infrastructure/
COPY src/EdgePulse.API/            EdgePulse.API/
RUN dotnet publish EdgePulse.API/EdgePulse.API.csproj -c Release -o /app --no-restore

# ── Runtime stage: the smaller ASP.NET runtime (no SDK) ──────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Listen on 8080 inside the container (the conventional non-root HTTP port for
# the .NET images). Map it to a host port when running, e.g. -p 5104:8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EdgePulse.API.dll"]
