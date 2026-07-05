# EdgePulse.TelemetryProcessor — multi-stage .NET 9 worker image
#
# Build CONTEXT must be the repo root (references Domain/Application/Infrastructure).
#
#   docker build -f src/backend/EdgePulse.TelemetryProcessor/Dockerfile -t edgepulse-telemetry-processor .

# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/backend/EdgePulse.Domain/EdgePulse.Domain.csproj                          EdgePulse.Domain/
COPY src/backend/EdgePulse.Application/EdgePulse.Application.csproj                  EdgePulse.Application/
COPY src/backend/EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj           EdgePulse.Infrastructure/
COPY src/backend/EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj   EdgePulse.TelemetryProcessor/
RUN dotnet restore EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj

COPY src/backend/EdgePulse.Domain/             EdgePulse.Domain/
COPY src/backend/EdgePulse.Application/         EdgePulse.Application/
COPY src/backend/EdgePulse.Infrastructure/     EdgePulse.Infrastructure/
COPY src/backend/EdgePulse.TelemetryProcessor/ EdgePulse.TelemetryProcessor/
RUN dotnet publish EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj -c Release -o /app --no-restore

# ── Runtime stage ────────────────────────────────────────────────────────────
# This is a background worker (no HTTP endpoint), so we use the plain .NET
# runtime image and expose no ports.
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

ENTRYPOINT ["dotnet", "EdgePulse.TelemetryProcessor.dll"]
