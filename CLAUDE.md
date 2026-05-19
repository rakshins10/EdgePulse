# EdgePulse -- Claude Code Instructions

Read this file every session. This is the complete context
for the EdgePulse project.

---

## Project Overview

EdgePulse is an Industrial IoT Device Management Platform.
Multi-tenant SaaS product targeting mid-market manufacturers.
Supports both cloud (Azure) and on-premise deployment.

Built by Rakshith N S -- 10 years at ABB Finland (Pulp & Paper MES).
Goal: Build a complete, sellable product. Not just a portfolio project.

GitHub:   https://github.com/rakshins10/EdgePulse
Local:    C:\Studies\EdgePulse-Application\EdgePulse
Board:    https://github.com/users/rakshins10/projects (EdgePulse Development)

---

## Developer Profile

Name:       Rakshith N S
Experience: 10+ years, .NET Full Stack
Current:    R&D Specialist at ABB Finland (Pulp & Paper MES)
Target:     Senior Engineer / Tech Lead / Principal Engineer roles
            Bosch, Siemens, Honeywell, Microsoft India
Relocating: India in 1-2 years

---

## Product Vision

The affordable, configurable, on-premise-capable industrial IoT
platform for mid-market manufacturers (50-500 employees) that
enterprise vendors ignore.

Five differentiators:
  1. On-premise first (works without internet)
  2. Fully configurable (every dropdown from UI)
  3. Industry specific (Pulp & Paper, Manufacturing templates)
  4. Affordable (500-2000 EUR/month vs 500,000 EUR/year competitors)
  5. Domain expertise (built by ABB industrial software engineer)

---

## Tech Stack

Backend API:      .NET 9, ASP.NET Core, Clean Architecture
                  CQRS + MediatR, EF Core 9, FluentValidation
Telemetry:        Node.js 20, NestJS, TypeScript
Processor:        .NET 9 Worker Service
Identity:         Keycloak 24 + PostgreSQL 16
                  Azure AD SSO (OIDC) + On-premise AD (LDAP)
Message Queue:    Azure Service Bus (cloud) / RabbitMQ (on-premise)
Telemetry DB:     Azure Cosmos DB (cloud) / MongoDB (on-premise)
Primary DB:       Azure SQL (cloud) / SQL Server 2022 (on-premise)
Load Balancer:    HAProxy (on-premise) / Azure Container Apps (cloud)
AI Cloud:         Azure OpenAI GPT-4o-mini
AI On-premise:    Ollama llama3.2
ML:               Azure ML (cloud) / ONNX runtime (on-premise)
Mobile:           React Native (iOS + Android) -- Phase 2
Frontend:         React 18, TypeScript, Tailwind CSS
CI/CD:            GitHub Actions + self-hosted runner
Secrets:          Azure Key Vault (cloud) / HashiCorp Vault (on-premise)
Monitoring:       Azure App Insights (cloud) / Grafana + Loki (on-premise)

---

## Organizational Hierarchy

Platform (EdgePulse SuperAdmin)
  -> Customer / Tenant (e.g. NordPulp Industries)
       -> Mill (e.g. Lakewood Mill, Tampere Finland)
            -> Area (e.g. Paper Machine 1)
                 -> Device (e.g. PUMP-LW-001)

---

## Roles (5 total)

SuperAdmin     -> Platform level, all access
CustomerAdmin  -> Tenant level, all mills
MillManager    -> Single mill only
Operator       -> Assigned areas only
Executive      -> Read only, tenant level

---

## Coding Standards (CRITICAL)

1. NO hardcoded values in business logic EVER
   C#: enums for fixed values, IOptions<T> for config
   Node.js: constants files, JSON config files

2. All secrets via Key Vault / environment variables

3. Clean Architecture layers strictly enforced

4. CQRS with MediatR
   Commands -> write operations
   Queries  -> read operations (never modifies state)

5. Global Query Filters in EF Core
   Auto-filter by TenantId + IsDeleted always

6. Soft delete only -- never hard delete

7. All timestamps in UTC

8. Lookup tables for ALL configurable values
   No hardcoded dropdowns anywhere in UI

9. Well-known GUIDs for seeded system values

---

## Local Infrastructure

Start:  docker compose -f infrastructure/docker-compose.onpremise.yml up -d
Stop:   docker compose -f infrastructure/docker-compose.onpremise.yml down

Service URLs:
  HAProxy Stats  http://localhost:8404/stats  admin/edgepulse123
  Keycloak       http://localhost:8080        admin/admin
  RabbitMQ UI    http://localhost:15672       edgepulse/EdgePulse@2026
  SQL Server     localhost:1433               sa/EdgePulse@2026
  MongoDB        localhost:27017              edgepulse/EdgePulse@2026
  Swagger        http://localhost:5104/swagger

---

## GitHub Project Board Workflow

BEFORE coding:
  1. Pick story from Backlog
  2. Move to In Progress on board
  3. Create branch: git checkout -b feature/US-XXX-description
  4. Assign yourself to the issue

WHILE coding:
  5. Commit with issue reference: git commit -m "feat: ... #XX"

AFTER coding:
  6. Test in Swagger
  7. Move to In Review on board
  8. Push + create PR: gh pr create (body must say Closes #XX)

AFTER testing:
  9. Merge PR -> issue auto-closes -> board moves to Done
  10. Clean up: git branch -d feature/US-XXX

---

## Current Status

PHASE 1 -- FOUNDATION (Sprints 1-10):

Sprint 1 -- Configuration Module: 70% complete
  DONE:
    GET /api/configuration/device-types        #11 closed
    POST/PUT/DELETE device-types               #12 #13 #14 closed
    GET /api/configuration/device-statuses     #15 closed
    POST/PUT/DELETE device-statuses            #16 closed
    GET /api/configuration/alert-severities    #17 closed
    GET /api/configuration/alert-statuses      #19 closed
    GET /api/configuration/industry-templates  #20 closed
    ExceptionHandlingMiddleware

  TODO (Sprint 1):
    POST/PUT/DELETE alert-severities           #18
    Manage all lookup types                    #21
    Tenant lookup overrides                    #22

Sprint 2 -- Organisation Module: TODO (#23-#26)
Sprint 3 -- Device Management: TODO (#27-#29)
Sprints 4-10: TODO

PHASE 2 (Sprints 11-13): TODO
  Mobile App, Predictive Maintenance, ESG Reporting

PHASE 3 (Sprints 14-16): TODO
  OPC-UA, Work Orders, Compliance Reports

PHASE 4 (Sprints 17-19): TODO
  Multi-Language, Digital Twin, Edge AI

PHASE 5 (Sprints 20-22): TODO
  API Marketplace, White Label, Go To Market

---

## Commit Message Format

feat/fix/docs/infra/test/chore/refactor: description #XX
PR body: "Closes #XX" to auto-close issue

---

## Key Documents

docs/01-requirements.md    -> requirements, roles, hierarchy
docs/02-architecture.md    -> architecture, HAProxy, ADRs
docs/03-data-design.md     -> schemas, Cosmos DB, ERD
ARCHITECTURE.md            -> solution architecture guide
PRODUCT-ROADMAP.md         -> full product roadmap Phases 1-5
CLAUDE-SETUP.md            -> all setup commands
DOCKER-COMMANDS.md         -> all Docker commands
