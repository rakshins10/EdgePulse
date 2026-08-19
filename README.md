# EdgePulse 🏭

> **Industrial IoT Device Management Platform**  
> Real-time telemetry ingestion · Anomaly detection · AI-powered alerts · Multi-tenant architecture

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Node.js](https://img.shields.io/badge/Node.js-20.x-339933?style=flat-square&logo=node.js)](https://nodejs.org)
[![NestJS](https://img.shields.io/badge/NestJS-10.x-E0234E?style=flat-square&logo=nestjs)](https://nestjs.com)
[![React](https://img.shields.io/badge/React-18.x-61DAFB?style=flat-square&logo=react)](https://reactjs.org)
[![Azure](https://img.shields.io/badge/Azure-Cloud-0078D4?style=flat-square&logo=microsoft-azure)](https://azure.microsoft.com)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?style=flat-square&logo=docker)](https://docker.com)
[![Keycloak](https://img.shields.io/badge/Keycloak-SSO-4D4D4D?style=flat-square&logo=keycloak)](https://keycloak.org)
[![License](https://img.shields.io/badge/License-CC%20BY--NC--ND%204.0-red?style=flat-square)](LICENSE)

---

## 📋 Table of Contents

- [Overview](#-overview)
- [Problem Statement](#-problem-statement)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Features](#-features)
- [Organizational Hierarchy](#-organizational-hierarchy)
- [Roles & Access Control](#-roles--access-control)
- [Getting Started](#-getting-started)
- [Project Structure](#-project-structure)
- [Documentation](#-documentation)
- [Roadmap](#-roadmap)
- [Author](#-author)

---

## 🔍 Overview

EdgePulse is a **multi-tenant Industrial IoT Device Management Platform** designed for large-scale industrial operations such as pulp & paper mills, manufacturing plants, and process industries.

It enables industrial organizations to:
- **Register and manage** all physical devices across multiple facilities from a single platform
- **Ingest high-frequency telemetry** from thousands of devices in real time
- **Detect anomalies automatically** using configurable threshold rules
- **Close the loop** — alerts open maintenance work orders and reach people via in-app, email and signed webhooks
- **Notify the right people** based on their role and operational scope
- **Visualize** device health and performance trends on a real-time dashboard

> **Real-world context:** This platform is modelled after industrial operations at companies like NordPulp Industries, AlpineBoard GmbH, and RiverPaper AG — managing multiple paper mills across different countries, each with hundreds of industrial devices.

---

## 🎯 Problem Statement

Industrial facilities operate hundreds of devices — pumps, motors, valves, compressors, sensors — generating continuous operational data. Traditional monitoring relies on:

- **Manual inspections** — operators physically check equipment
- **Reactive maintenance** — faults are discovered after failure
- **Fragmented visibility** — no single view across multiple facilities

By the time a problem is identified, significant damage, unplanned downtime, or safety incidents may have already occurred.

**EdgePulse solves this** by continuously monitoring every device, detecting anomalies in real time, and alerting the right people before failures happen.

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         EdgePulse Platform                      │
│                                                                 │
│  ┌──────────────────┐        ┌──────────────────────────────┐   │
│  │  React Dashboard │        │   Keycloak Identity Provider │   │
│  │  (TypeScript)    │◄──────►│   + PostgreSQL               │   │
│  │  Azure Static    │        │   Azure AD SSO (OIDC)        │   │
│  │  Web Apps        │        └──────────────────────────────┘   │
│  └────────┬─────────┘                    │ JWT                  │
│           │ REST                         ▼                      │
│           ▼                  ┌───────────────────────┐          │
│  ┌─────────────────┐         │   Device API          │          │
│  │  Telemetry Svc  │         │   (.NET 9 / ASP.NET)  │          │
│  │  (Node.js /     │         │   Clean Architecture  │          │
│  │   NestJS)       │         │   EF Core 9           │          │
│  └────────┬────────┘         │   Azure SQL           │          │
│           │ publish          └───────────────────────┘          │
│           ▼                                                     │
│  ┌─────────────────┐                                            │
│  │ Azure Service   │                                            │
│  │ Bus Queue       │                                            │
│  └────────┬────────┘                                            │
│           │ consume                                             │
│           ▼                                                     │
│  ┌─────────────────┐        ┌──────────────────────────────┐    │
│  │ Processor Svc   │───────►│   Azure Cosmos DB            │    │
│  │ (.NET 9 Worker) │        │   (Telemetry Storage)        │    │
│  │ Anomaly detect  │        └──────────────────────────────┘    │
│  │ + alert engine  │                                            │
│  └─────────────────┘                                            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │  Azure Key Vault · App Insights · Container Apps         │   │
│  │  GitHub Actions CI/CD · Docker · Self-hosted Runner      │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
Device sends telemetry
        │
        ▼
Telemetry Service (Node.js)
  → Validates API key
  → Validates payload
  → Publishes to Service Bus
        │
        ▼
Azure Service Bus Queue
  (buffers messages, handles spikes)
        │
        ▼
Processor Service (.NET Worker)
  → Reads from queue
  → Checks against thresholds
  → Detects anomaly?
        ├── NO  → Store telemetry in Cosmos DB
        └── YES → Create alert
                → Store in Cosmos DB
                → Send notifications (in-app + email + webhooks)
                        │
                        ▼ (later, on demand — never in the hot path)
        Operator clicks "✦ Explain" on the alert
          → API builds a prompt from the alert facts + recent readings
          → Local LLM (Ollama / llama3.2) or Azure OpenAI writes
            WHAT HAPPENED / LIKELY CAUSES / RECOMMENDED ACTION
          → Cached on the alert (Alert.AiSummary)
```

---

## 🛠️ Tech Stack

EdgePulse is **on-premise first**. The left column is what ships and is verified in this repo; the cloud profile maps 1-to-1 and is selected by `DEPLOYMENT_MODE` (see [Deployment guide](docs/reference/deployment.md)).

| Layer | On-premise (shipped, v1.0) | Cloud profile (Azure) |
|-------|---------------------------|-----------------------|
| **API** | .NET 9, ASP.NET Core — Clean Architecture, CQRS + MediatR, FluentValidation | same |
| **ORM** | Entity Framework Core 9 (retry-on-failure, automatic audit capture) | same |
| **Telemetry ingestion** | OPC-UA edge agent (node-opcua) + NestJS REST ingest | same |
| **Background processor** | .NET 9 Worker Service — alert engine + notification fan-out | same |
| **Identity** | Keycloak 24 + PostgreSQL 16 — OIDC, JWT, RBAC, Admin API | Keycloak on Container Apps / Entra External ID |
| **Message queue** | RabbitMQ 3.12 | Azure Service Bus |
| **Relational DB** | SQL Server 2022 | Azure SQL |
| **Time-series DB** | MongoDB 7 (aggregation pipelines for energy/health) | Cosmos DB (Mongo API) |
| **File storage** | Local volume behind `IFileStorage` | Azure Blob |
| **Email** | SMTP (MailHog locally) | ACS Email / SendGrid |
| **Secrets** | `dotnet user-secrets` (dev) / env vars (Docker) — nothing in git | Azure Key Vault + Managed Identity |
| **Frontend** | React 18, TypeScript, Vite, CSS Modules, React Query, i18next (en/fi/sv) | same |
| **Load balancer** | HAProxy | Azure Container Apps ingress |
| **CI/CD** | GitHub Actions → GHCR (per-component versioning, beta + release channels) | same |
| **Containers** | Docker Compose (full on-prem stack) | Container Apps |

---
## ✨ Features

Everything below is **shipped in v1.0.0** and verified live end-to-end.

### 🔐 Identity & Access Management
- **Keycloak** OIDC — JWT auth for the API, SSO for the dashboard
- **5-level RBAC** — SuperAdmin, CustomerAdmin, MillManager, Operator, Executive; every handler is role-guarded and unit-tested
- **Tenant isolation** — all queries scoped by the JWT `tenantId` claim; MillManager/Operator further scoped to their mill/areas
- **User management UI** — create users, assign roles + mill/area scope, enable/disable, temporary passwords (Keycloak Admin API)
- Azure AD SSO and on-prem AD/LDAP via Keycloak federation (documented; not app code)

### 🏭 Device Management
- Register (one-time hashed API key), edit, decommission (revokes keys, keeps telemetry)
- Everything-is-data lookups: device types, statuses, maintenance/metric/location types — tenant custom values, protected system values
- **File attachments** — manuals, datasheets, CAD (25 MB, allow-listed types, role-gated)
- **Live 2D floor plan** — mill map with colour-coded device health, drag-to-place layout editing

### 📡 Telemetry Ingestion
- **OPC-UA edge agent** with **auto-discovery** (`npm run discover` browses a server and generates the device/metric mapping) + a full NordPulp plant simulator
- REST ingestion endpoint (`X-Device-Key`) for anything that can POST JSON
- RabbitMQ → .NET Telemetry Processor → MongoDB time-series; live Recharts per metric

### 🚨 Alerts, Notifications & Work Orders
- Configurable per-device thresholds; fires after **3 consecutive** breaches (noise filter); one open alert per (device, metric, threshold)
- Lifecycle Open → Acknowledged → Resolved with audit fields
- **Delivery on every alert:** in-app notification bell (deep-links to the record) + SMTP email + HMAC-signed webhooks (Slack/Teams-compatible)
- **Maintenance work orders** auto-opened from CRITICAL/HIGH alerts — guarded lifecycle, assignment, parts + completion notes, per-device history

### 📊 Dashboards, Reports & Analytics
- Role-scoped KPI dashboard and executive view
- **Cross-mill comparison** with MTTA/MTTR + CSV exports (comparison + alert detail)
- **Energy & ESG** — kWh and CO₂e from power telemetry, per-mill/device breakdowns, daily chart, ESG CSV (GHG Protocol Scope 2)
- **Device health scoring** — transparent statistical condition score + linear days-to-threshold indicator, worst-first board

### 🔍 Compliance & Platform
- **Audit trail** — every create/update/delete captured automatically with property-level old→new diffs; admin page + CSV evidence export
- **Localization** — full UI in English, Finnish, Swedish; locales are data (add any language in-app, DB-backed strings, CSV translation round-trip)
- **White-label branding** — per-tenant product name, logo, accent colour applied live
- Independent per-component versioning, changelog-driven beta images, tag-cut releases to GHCR

---

## 🏢 Organizational Hierarchy

EdgePulse models real industrial organizations with a 4-level hierarchy:

```
Platform (EdgePulse)
└── Customer (e.g. NordPulp Industries)
      └── Mill (e.g. Lakewood Mill, Finland)
            └── Area (e.g. Paper Machine 1)
                  └── Device (e.g. PUMP-LW-001)
```

**Example — NordPulp Industries:**

```
NordPulp Industries
├── Lakewood Mill (Tampere, Finland)
│     ├── Paper Machine 1 → PUMP-LW-001, MOTOR-LW-001, SENSOR-LW-001
│     ├── Paper Machine 2 → PUMP-LW-002, VALVE-IM-001
│     └── Pulp Processing → PUMP-LW-003, SENSOR-LW-002
│
└── Riverside Mill (Gothenburg, Sweden)
      ├── Paper Machine 1 → PUMP-RV-001, MOTOR-RV-001, SENSOR-RV-001
      ├── Paper Machine 2 → PUMP-RV-002, SENSOR-RV-002
      └── Water Treatment → PUMP-RV-003, VALVE-RV-001, SENSOR-RV-003
```

---

## 👥 Roles & Access Control

| Role | Scope | Key Permissions |
|------|-------|----------------|
| **SuperAdmin** | Platform | Create tenants, impersonate, full access |
| **Customer Admin** | Tenant | Manage mills, users, roles — all mills |
| **Mill Manager** | Single Mill | Manage devices, areas, alerts — one mill |
| **Operator** | Assigned Areas | View & acknowledge alerts — assigned areas only |
| **Executive** | Tenant (read-only) | View dashboards, cross-mill reports — no actions |

---

## 🚀 Getting Started

### Prerequisites

| Tool | Version | Check |
|------|---------|-------|
| Docker Desktop | Any recent | `docker --version` |
| .NET SDK | 9.x | `dotnet --version` |
| Node.js | 20.x | `node --version` |
| npm | 10.x+ | `npm --version` |

### Service Ports (Local Dev)

| Service | URL | Where it runs |
|---------|-----|---------------|
| Dashboard (Vite) | http://localhost:3000 | `npm run dev` |
| EdgePulse.API | http://localhost:5104 | `dotnet run` |
| API Swagger | http://localhost:5104/swagger | (built into API) |
| Keycloak Admin | http://localhost:8080 | Docker (admin / admin) |
| RabbitMQ UI | http://localhost:15672 | Docker (edgepulse / EdgePulse@2026) |
| SQL Server | localhost:1433 | Docker (sa / EdgePulse@2026) |
| MongoDB | localhost:27017 | Docker (edgepulse / EdgePulse@2026) |
| OPC-UA Server | opc.tcp://localhost:4840 | Docker (anonymous) |
| HAProxy Stats | http://localhost:8404/stats | Docker (admin / edgepulse123) |

### Quick Start (NordPulp demo — all 20 devices)

```powershell
# 1. Clone
git clone https://github.com/rakshins10/EdgePulse.git
cd EdgePulse

# 2. Start infrastructure (SQL Server, MongoDB, RabbitMQ, Keycloak)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d `
    sqlserver mongodb rabbitmq postgres keycloak mailhog

# 3. One-time: application secrets (committed appsettings hold placeholders;
#    the services refuse to start without these — see docs/guides/01-setup-guide.md §4)
$API="src/backend/EdgePulse.API/EdgePulse.API.csproj"
$TP="src/backend/EdgePulse.TelemetryProcessor/EdgePulse.TelemetryProcessor.csproj"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True;" --project $API
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $API
dotnet user-secrets set "Keycloak:ClientSecret" "<from Keycloak: Clients > edgepulse-api > Credentials>" --project $API
dotnet user-secrets set "Keycloak:AdminPassword" "admin" --project $API
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=localhost,1433;Database=EdgePulse;User Id=sa;Password=EdgePulse@2026;TrustServerCertificate=True" --project $TP
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://edgepulse:EdgePulse%402026@localhost:27017" --project $TP
dotnet user-secrets set "ConnectionStrings:RabbitMQ" "amqp://edgepulse:EdgePulse%402026@localhost:5672/edgepulse" --project $TP

# 4. Apply EF migrations + seed NordPulp demo data
dotnet run --project src/backend/EdgePulse.API -- --seed

# 5. Start the API (new terminal — leave running)
dotnet run --project src/backend/EdgePulse.API

# 6. Start the TelemetryProcessor (new terminal — leave running)
dotnet run --project src/backend/EdgePulse.TelemetryProcessor

# 7. Start the OPC-UA simulator + agent (publishes telemetry to RabbitMQ)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d `
    opcua-simulator opcua-agent

# 8. Start the Dashboard (new terminal — leave running)
cd src/EdgePulse.Dashboard
npm install   # first time only
npm run dev
```

Open **http://localhost:3000** in a browser.

### First-time Keycloak Setup

Keycloak needs a one-time configuration:

1. Open http://localhost:8080 (admin / admin)
2. Import the `edgepulse` realm (or create manually with the `edgepulse-dashboard` public client)
3. **Create custom protocol mappers** on the `edgepulse-dashboard` client (Client scopes → dedicated scope → Add mapper → User Attribute):
   - `tenantId` (required — without this, the dashboard shows zeros)
   - `role`, `millId`, `areaIds`
4. Create your user and set the `tenantId` attribute to `10000001-0000-0000-0000-000000000001` (the seeded NordPulp tenant)
5. Assign realm role: `SuperAdmin`

> Full step-by-step setup + troubleshooting: **[docs/testing/local-test-guide.md](docs/testing/local-test-guide.md)**

### Stopping Everything

```powershell
# Stop local dotnet/npm processes: Ctrl+C in each terminal

# Stop Docker services (data persists)
docker compose -f infrastructure/docker-compose.onpremise.yml down

# Stop and DELETE all data (full reset)
docker compose -f infrastructure/docker-compose.onpremise.yml down -v
```

---

## 📁 Project Structure

```
EdgePulse/
│
├── src/
│   ├── backend/                        # All .NET 9 — one "backend" release line
│   │   ├── EdgePulse.Domain/           # Entities with behaviour, no framework refs
│   │   ├── EdgePulse.Application/      # CQRS handlers (MediatR), validators, pure logic
│   │   ├── EdgePulse.Infrastructure/   # EF Core + SQL Server, migrations, Keycloak admin,
│   │   │                               #   file storage, webhook sender, audit capture
│   │   ├── EdgePulse.API/              # REST API (JWT, Swagger, Mongo read-side)
│   │   ├── EdgePulse.TelemetryProcessor/  # RabbitMQ → MongoDB + alert engine + fan-out
│   │   ├── EdgePulse.sln · Directory.Build.props · CHANGELOG.md
│   │
│   ├── EdgePulse.Dashboard/            # React 18 + TypeScript + Vite (CSS Modules)
│   ├── EdgePulse.Ingestion/            # NestJS REST telemetry ingestion
│   └── EdgePulse.OpcUaAgent/           # OPC-UA edge agent + simulator + auto-discovery
│
├── tests/
│   ├── EdgePulse.Domain.Tests/         # xUnit — entity behaviour
│   └── EdgePulse.Application.Tests/    # xUnit — handlers (EF InMemory + NSubstitute)
│
├── infrastructure/
│   └── docker-compose.onpremise.yml    # SQL Server, MongoDB, RabbitMQ, Postgres, Keycloak,
│                                       #   MailHog, HAProxy, OPC-UA simulator + agent
├── .github/
│   ├── workflows/                      # ci.yml + publish-{backend,dashboard,ingestion,opcua-agent}.yml
│   └── actions/component-version/      # changelog-driven version resolution
│
├── docs/
│   ├── guides/                         # Setup · Configuration · Functionality · Technical
│   ├── reference/                      # API · Auth · Deployment · Integrations · Operations · Strategy
│   ├── devops/                         # CI/CD guide · Releasing guide
│   ├── sprints/                        # Per-sprint delivery journals (4 → 28)
│   └── 01-requirements · 02-architecture · 03-data_design … (original design docs)
│
├── ARCHITECTURE.md · PRODUCT-ROADMAP.md · DOCKER-COMMANDS.md
└── README.md
```

---

## 📄 Documentation

**Start here** — the five end-to-end guides cover everything needed to understand, configure and run the platform from scratch:

| Guide | What it covers |
|-------|----------------|
| [Setup Guide](docs/guides/01-setup-guide.md) | Install & run everything from a fresh clone, verify the pipeline, troubleshoot |
| [Configuration Guide](docs/guides/02-configuration-guide.md) | Every setting — appsettings, secrets, env, compose — plus all in-product configuration |
| [Functionality Guide](docs/guides/03-functionality-guide.md) | Every module, role permissions, ingestion paths |
| [Technical Guide](docs/guides/04-technical-guide.md) | Full frontend + backend architecture breakdown |
| [AI Guide](docs/guides/05-ai-guide.md) | Beginner-level explanation of the AI features: LLM concepts, Ollama, prompts, design decisions, running & tuning |

**Reference**

| Document | Description |
|----------|-------------|
| [API Reference](docs/reference/api-reference.md) | Endpoint catalogue with role legend (Swagger UI is canonical) |
| [Authentication & AD/LDAP](docs/reference/authentication.md) | Keycloak, claims model, Azure AD SSO, LDAP federation |
| [Integrations](docs/reference/integrations.md) | OPC-UA + auto-discovery, simulator, REST ingest, signed webhooks |
| [Deployment](docs/reference/deployment.md) | Local, on-premise Docker, cloud mapping, CI/CD |
| [Operations](docs/reference/operations.md) | Monitoring, backup, security-hardening checklist, upgrades |
| [Strategy](docs/reference/strategy.md) | GTM summary — full narrative in `PRODUCT-ROADMAP.md` |
| [Keycloak Setup](docs/keycloak-setup.md) | Realm, clients, protocol mappers, demo users |
| [Demo Data Setup](docs/domain/02-demo-data-setup.md) | The NordPulp seed: fixed GUIDs, devices, thresholds |
| [Local Test Guide](docs/testing/local-test-guide.md) | Manual test walkthrough of the full stack |

**Engineering & process**

| Document | Description |
|----------|-------------|
| [Project Guide](docs/project-guide.md) | Engineering standards, architecture rules, conventions |
| [Implementation Patterns](docs/implementation-patterns.md) | Code patterns used across handlers, controllers, UI |
| [Development Setup](docs/development-setup.md) | Toolchain, commands, and the original scaffolding journal |
| [CI/CD Guide](docs/devops/01-cicd-guide.md) | GitHub Actions + GHCR, beginner-oriented |
| [Releasing Guide](docs/devops/02-releasing.md) | Per-component versioning, changelog-driven betas, cutting a release |
| [Sprint History](docs/sprint-history.md) | Chronological journal; per-sprint detail in [`docs/sprints/`](docs/sprints/) |

**Original design documents** (written before Sprint 1 — point-in-time, kept for provenance)

| Document | Description |
|----------|-------------|
| [Requirements](docs/01-requirements.md) | Functional & non-functional requirements |
| [Architecture](docs/02-architecture.md) | System design & component architecture |
| [Data Design](docs/03-data_design.md) | Database schemas & data flow |

---

## 🗺️ Roadmap

### ✅ v1.0.0 — delivered (Sprints 1–28, released 2026-07-24)
All four components are tagged `*-v1.0.0` with images on GHCR. Highlights:
- Configuration system, organisation hierarchy, device management with attachments
- Keycloak identity + RBAC + **user management UI**
- Telemetry pipeline: OPC-UA agent **with auto-discovery**, REST ingest, RabbitMQ → MongoDB
- Alert engine with **in-app / email / signed-webhook delivery** and **auto-created work orders**
- Dashboards, **cross-mill reports (MTTA/MTTR)**, **Energy & ESG**, **device health scoring**, **live 2D floor plan**
- **Audit trail**, **white-label branding**, en/fi/sv localization
- 130 unit tests in CI, per-component versioning, complete documentation suite

### v1.0.1 — hardening (in progress)
- [x] Application secrets out of git (`dotnet user-secrets` / env vars; services fail fast on placeholders)
- [x] Demo role users homed in the NordPulp tenant with correct scoping
- [ ] Remaining production checklist — see [Operations guide](docs/reference/operations.md)

### v1.1.0 — AI features (in progress)
- [x] **Sprint 29 — AI alert explanations**: ✦ Explain on every alert (WHAT HAPPENED / LIKELY CAUSES / RECOMMENDED ACTION), on-demand + cached, on-prem **Ollama (llama3.2)** in compose or Azure OpenAI via config, graceful degradation — [#9](https://github.com/rakshins10/EdgePulse/issues/9), [#39](https://github.com/rakshins10/EdgePulse/issues/39). See the [AI Guide](docs/guides/05-ai-guide.md).
- [ ] **Sprint 30 — natural-language device Q&A** (grounded in live device/alert data)

### Post-v1.1 (deliberately deferred)
- **Mobile app** (React Native) — [#31](https://github.com/rakshins10/EdgePulse/issues/31)
- **Commercialisation** — website, self-service trial, billing — [#42](https://github.com/rakshins10/EdgePulse/issues/42)
- Trained ML models, 3D digital twin, pre-built SAP/ServiceNow connectors — each has its v1.0 foundation shipped

> Full product vision and market strategy: `PRODUCT-ROADMAP.md`.

---

## 👨‍💻 Author

**Rakshith N S**  
R&D Specialist | Senior .NET Full Stack Engineer  
ABB Finland — Pulp & Paper MES, Digital  

[![LinkedIn](https://img.shields.io/badge/LinkedIn-rakshith--n--s-0A66C2?style=flat-square&logo=linkedin)](https://linkedin.com/in/rakshith-n-s)
[![GitHub](https://img.shields.io/badge/GitHub-rakshins10-181717?style=flat-square&logo=github)](https://github.com/rakshins10)

---

## 📜 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## ⚠️ License & Usage

Copyright (c) 2026 **Rakshith N S**. All rights reserved.

This project is licensed under [CC BY-NC-ND 4.0](LICENSE).

```
✅ You MAY view and reference this project for learning purposes
✅ You MAY share a link to this repository
❌ You MAY NOT copy, clone, or redistribute this code
❌ You MAY NOT use this commercially
❌ You MAY NOT modify and redistribute this work
```

> This project represents original work developed as part of a personal
> career portfolio. Unauthorized copying or redistribution is prohibited.

---

<div align="center">
  <sub>Built with heart to demonstrate real-world industrial IoT architecture</sub>
</div>
