# EdgePulse -- Claude Code Instructions

Read this file every session. This is the complete context
for the EdgePulse project.

---

## Project Overview

EdgePulse is an Industrial IoT Device Management Platform.
Multi-tenant, supports both cloud (Azure) and on-premise deployment.
Built by Rakshith N S as a portfolio project targeting
Senior Engineer / Tech Lead / Principal Engineer roles at
Bosch, Siemens, Honeywell, Microsoft India.

GitHub: https://github.com/rakshins10/EdgePulse
Local:  C:\Studies\EdgePulse-Application

---

## Developer Profile

Name:       Rakshith N S
Experience: 10+ years, .NET Full Stack
Current:    R&D Specialist at ABB Finland (Pulp & Paper MES)
Target:     Senior Engineer / Tech Lead roles in India
            Target companies: Bosch, Siemens, Honeywell,
            Microsoft India, Rockwell Automation

---

## Tech Stack

Backend API:      .NET 9, ASP.NET Core, Clean Architecture
                  CQRS + MediatR, EF Core 9
Telemetry:        Node.js 20, NestJS, TypeScript
Processor:        .NET 9 Worker Service
Identity:         Keycloak 24 + PostgreSQL 16
                  Azure AD SSO (OIDC) + On-premise AD (LDAP)
Message Queue:    Azure Service Bus (cloud)
                  RabbitMQ (on-premise)
Telemetry DB:     Azure Cosmos DB (cloud)
                  MongoDB 7.0 (on-premise)
Primary DB:       Azure SQL (cloud)
                  SQL Server 2022 (on-premise)
Load Balancer:    HAProxy (on-premise)
                  Azure Container Apps (cloud)
AI:               Azure OpenAI GPT-4o-mini (cloud)
                  Ollama llama3.2 (on-premise)
Frontend:         React 18, TypeScript
CI/CD:            GitHub Actions + self-hosted runner
Secrets:          Azure Key Vault (cloud)
                  HashiCorp Vault (on-premise)
Monitoring:       Azure App Insights (cloud)
                  Grafana + Loki (on-premise)

---

## Organizational Hierarchy

Platform (EdgePulse)
  -> Customer / Tenant (e.g. NordPulp Industries)
       -> Mill (e.g. Lakewood Mill, Tampere Finland)
            -> Area (e.g. Paper Machine 1)
                 -> Device (e.g. PUMP-LW-001)

---

## Roles (5 total)

SuperAdmin     -> Platform level, all access
CustomerAdmin  -> Tenant level, all mills
MillManager    -> Single mill only
Operator       -> Assigned areas only (one mill max)
Executive      -> Read only, tenant level

---

## Coding Standards (CRITICAL)

1. NO hardcoded values in business logic EVER
   C#: use enums for fixed values, IOptions<T> for config
   Node.js: use constants files, JSON config files

2. All secrets via Key Vault / environment variables
   Never in appsettings.json or committed to git

3. Clean Architecture layers (Device API):
   Domain        -> entities, enums, value objects
   Application   -> commands, queries, handlers, interfaces
   Infrastructure-> EF Core, Azure SDKs, external services
   WebAPI        -> controllers, middleware, DI setup

4. CQRS pattern using MediatR
   Commands -> write operations (INSERT, UPDATE, DELETE)
   Queries  -> read operations (SELECT)

5. Global Query Filters in EF Core
   All queries automatically filtered by TenantId
   IsDeleted = false filter applied globally
   Never write manual WHERE TenantId = ... 

6. Soft delete only -- never hard delete
   IsDeleted = true, DeletedAt = timestamp

7. All timestamps in UTC
   Display in mill local timezone (from Mills.Timezone)

8. ASCII diagrams only -- no Unicode box characters

---

## Local Infrastructure (Docker)

Start:  docker compose -f infrastructure/docker-compose.onpremise.yml up -d
Stop:   docker compose -f infrastructure/docker-compose.onpremise.yml down
Status: docker compose -f infrastructure/docker-compose.onpremise.yml ps

Service URLs:
  HAProxy Stats  http://localhost:8404/stats  admin/edgepulse123
  Keycloak       http://localhost:8080        admin/admin
  RabbitMQ UI    http://localhost:15672       edgepulse/EdgePulse@2026
  SQL Server     localhost:1433               sa/EdgePulse@2026
  MongoDB        localhost:27017              edgepulse/EdgePulse@2026

---

## Project Structure (target)

EdgePulse/
  src/
    EdgePulse.Domain/           <- entities, enums, interfaces
    EdgePulse.Application/      <- CQRS, MediatR, DTOs
    EdgePulse.Infrastructure/   <- EF Core, Azure, email
    EdgePulse.API/              <- ASP.NET Core, controllers
    EdgePulse.TelemetryService/ <- Node.js / NestJS
    EdgePulse.Processor/        <- .NET Worker Service
    EdgePulse.Dashboard/        <- React + TypeScript
  tools/
    DeviceSimulator/            <- simulates device telemetry
  infrastructure/
    docker-compose.onpremise.yml
    docker-compose.cloud.yml    <- TODO
    haproxy/haproxy.cfg
    mongo/init.js
    sql/                        <- migration scripts
  docs/
    01-requirements.md          <- DONE
    02-architecture.md          <- DONE
    03-data-design.md           <- DONE
    04-api-design.md            <- TODO
    05-identity-design.md       <- TODO
    06-infrastructure.md        <- TODO
  DOCKER-COMMANDS.md
  README.md
  LICENSE
  CLAUDE.md                     <- this file

---

## Current Status

COMPLETED:
  -> Documentation: requirements, architecture, data design
  -> Local infrastructure: Docker stack running
  -> GitHub repo: private, 7 commits

IN PROGRESS:
  -> Starting Device API (.NET 9)

TODO (in order):
  1. Create .NET 9 solution structure (Clean Architecture)
  2. Domain entities with enums (Device, Mill, Area, Alert)
  3. EF Core setup + first migration
  4. First API endpoint: POST /api/devices
  5. Keycloak realm configuration
  6. JWT validation middleware
  7. Node.js Telemetry Service
  8. Azure Service Bus integration
  9. .NET Processor Worker Service
  10. React Dashboard
  11. GitHub Actions CI/CD
  12. Azure deployment

---

## Commit Message Format

feat:  new feature
fix:   bug fix
docs:  documentation only
infra: infrastructure / docker / config
test:  tests
chore: maintenance, cleanup
refactor: code restructure without feature change

Examples:
  feat: add device registration endpoint
  fix: resolve JWT validation on expired token
  docs: add API design document v1.0
  infra: add cloud docker compose stack

---

## Key Design Decisions

1. Two deployment modes: cloud (Azure) and on-premise (Docker)
   Same Docker images, different infrastructure
   DEPLOYMENT_MODE env variable switches DI registrations

2. HAProxy for on-premise load balancing
   Active/Drain/Inactive states for zero-downtime deployment

3. Keycloak as identity broker
   Supports Azure AD (OIDC) and on-premise AD (LDAP)
   No internet needed for LDAP federation

4. Cosmos DB partition key = deviceId
   All readings for one device on same partition
   Fast time-range queries per device

5. EF Core Global Query Filters enforce tenant isolation
   Developer cannot forget WHERE TenantId clause

6. Service Bus queue between Telemetry and Processor
   Decouples ingestion speed from processing speed
   Messages survive Processor restart

7. 3 consecutive threshold breaches trigger alert
   Reduces false positives from sensor noise

8. Device API keys hashed (SHA-256)
   Plain text shown once at generation, then discarded
   Same principle as password hashing