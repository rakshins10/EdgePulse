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
- **Generate AI-powered alert summaries** using Azure OpenAI
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
│  │ + OpenAI alerts │                                            │
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
        └── YES → Generate AI alert summary (Azure OpenAI)
                → Create alert
                → Store in Cosmos DB
                → Send notifications (in-app + email)
```

---

## 🛠️ Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **Device API** | .NET 9, ASP.NET Core | Device management, user auth, REST API |
| **Architecture** | Clean Architecture, CQRS, MediatR | Maintainable, testable code structure |
| **ORM** | Entity Framework Core 9 | Database access for Device API |
| **Telemetry Service** | Node.js 20, NestJS, TypeScript | High-frequency telemetry ingestion |
| **Background Processor** | .NET 9 Worker Service | Queue processing, anomaly detection |
| **Identity** | Keycloak 24 + PostgreSQL 16 | SSO, JWT, RBAC, Azure AD integration |
| **Message Queue** | Azure Service Bus | Async telemetry processing, buffering |
| **Relational DB** | Azure SQL | Devices, users, alerts, audit logs |
| **Time-series DB** | Azure Cosmos DB | Telemetry storage (partitioned by deviceId) |
| **AI** | Azure OpenAI (GPT-4o-mini) | Human-readable alert summaries |
| **Secrets** | Azure Key Vault + Managed Identity | Zero hardcoded secrets |
| **Monitoring** | Azure Application Insights | Distributed tracing, metrics, logs |
| **Frontend** | React 18, TypeScript | Real-time dashboard |
| **Hosting** | Azure Container Apps | Scalable container hosting |
| **CI/CD** | GitHub Actions | Automated build, test, deploy |
| **Containers** | Docker, Docker Compose | Local dev and production packaging |

---

## ✨ Features

### 🔐 Identity & Access Management
- **Keycloak SSO** — single sign-on across all services
- **Azure Active Directory integration** — enterprise SSO via OIDC
- **5-level RBAC** — SuperAdmin, Customer Admin, Mill Manager, Operator, Executive
- **Tenant isolation** — row-level security, zero cross-tenant data access
- **MFA support** — via Keycloak

### 🏭 Device Management
- Register, update, and decommission industrial devices
- Device types: Pump, Motor, Valve, Sensor, Compressor, Fan
- Device status tracking: Online, Offline, Maintenance, Decommissioned
- Full device history: telemetry, alerts, maintenance events
- Search and filter by type, area, mill, status

### 📡 Telemetry Ingestion
- REST API endpoint with per-device API key authentication
- Supports: temperature, pressure, vibration, flow rate, power consumption
- Handles 1,000+ messages/minute per tenant
- Publishes to Azure Service Bus for reliable async processing

### 🚨 Anomaly Detection & Alerts
- Configurable thresholds per device per metric
- Triggers after **3 consecutive** threshold breaches (reduces false positives)
- Alert severity: Critical, High, Medium, Low
- **AI-generated alert summaries** via Azure OpenAI
- Alert lifecycle: Open → Acknowledged → Assigned → Resolved → Closed
- Email + in-app notifications (Critical and High severity)

### 📊 Dashboard & Reporting
- Real-time device status overview
- Live telemetry charts (last 24 hours per device)
- Active alert log with AI summaries
- Mill-level reports: uptime, alert frequency, telemetry trends
- Cross-mill comparison reports (all metrics, custom configurable)
- Export as PDF and CSV

### 🔍 Audit Trail
- Immutable log of all user actions
- Retained for 24 months
- Accessible to SuperAdmin, Customer Admin, Mill Manager

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
    sqlserver mongodb rabbitmq postgres keycloak

# 3. Apply EF migrations + seed NordPulp demo data
dotnet run --project src/backend/EdgePulse.API -- --seed

# 4. Start the API (new terminal — leave running)
dotnet run --project src/backend/EdgePulse.API

# 5. Start the TelemetryProcessor (new terminal — leave running)
dotnet run --project src/backend/EdgePulse.TelemetryProcessor

# 6. Start the OPC-UA simulator + agent (publishes telemetry to RabbitMQ)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d `
    opcua-simulator opcua-agent

# 7. Start the Dashboard (new terminal — leave running)
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
│   ├── EdgePulse.API/              # .NET 9 Device Management API
│   │   ├── Domain/                 # Entities, value objects
│   │   ├── Application/            # CQRS handlers, interfaces
│   │   ├── Infrastructure/         # EF Core, Azure services
│   │   └── API/                    # Controllers, middleware
│   │
│   ├── EdgePulse.TelemetryService/ # Node.js / NestJS ingestion service
│   │   ├── src/
│   │   │   ├── telemetry/          # Telemetry module
│   │   │   ├── auth/               # API key validation
│   │   │   └── queue/              # Service Bus publisher
│   │   └── Dockerfile
│   │
│   ├── EdgePulse.Processor/        # .NET 9 Worker Service
│   │   ├── Consumers/              # Service Bus consumers
│   │   ├── Detectors/              # Anomaly detection logic
│   │   ├── AI/                     # Azure OpenAI integration
│   │   └── Dockerfile
│   │
│   └── EdgePulse.Dashboard/        # React + TypeScript frontend
│       ├── src/
│       │   ├── pages/              # Dashboard, Devices, Alerts
│       │   ├── components/         # Reusable UI components
│       │   └── services/           # API clients
│       └── Dockerfile
│
├── docs/                           # Project documentation
│   ├── 01-requirements.md          # Requirements document ✅
│   ├── 02-architecture.md          # Architecture document (in progress)
│   ├── 03-data-design.md           # Data design (coming soon)
│   ├── 04-api-design.md            # API design (coming soon)
│   ├── 05-identity-design.md       # Identity design (coming soon)
│   └── 06-infrastructure.md        # Infrastructure design (coming soon)
│
├── infrastructure/
│   ├── docker-compose.yml          # Local development stack
│   ├── docker-compose.override.yml # Local overrides
│   └── bicep/                      # Azure infrastructure as code
│
├── .github/
│   └── workflows/
│       ├── api.yml                 # Device API CI/CD
│       ├── telemetry.yml           # Telemetry Service CI/CD
│       └── processor.yml           # Processor CI/CD
│
└── README.md
```

---

## 📄 Documentation

| Document | Status | Description |
|----------|--------|-------------|
| [Requirements](docs/01-requirements.md) | ✅ Complete | Functional & non-functional requirements |
| [Architecture](docs/02-architecture.md) | ✅ Complete | System design & component architecture |
| [Data Design](docs/03-data_design.md) | ✅ Complete | Database schemas & data flow |
| [Sprint History](docs/sprint-history.md) | ✅ Ongoing | Per-sprint journal; details in `docs/sprints/` |
| [Keycloak Setup](docs/keycloak-setup.md) | ✅ Complete | Realm, clients, protocol mappers, test users |
| [Local Test Guide](docs/testing/local-test-guide.md) | ✅ Complete | Run the full stack locally, step by step |
| [CI/CD Guide](docs/devops/01-cicd-guide.md) | ✅ Complete | GitHub Actions + GHCR, beginner-oriented |
| API Reference | 📋 Planned | Per-endpoint REST docs (issue #78) |
| Operations Guide | 📋 Planned | Monitoring, backup, hardening (issue #77) |

---

## 🗺️ Roadmap

### Delivered (Sprints 1–16)
- [x] Configuration module — configurable lookup tables (industry templates + tenant overrides)
- [x] Organisation hierarchy — Tenant → Mill → Area, role-scoped
- [x] Device management — registration, hashed API keys, full CRUD
- [x] Keycloak JWT auth — 5 roles, tenant/mill/area-scoped claims
- [x] Telemetry pipeline — NestJS ingestion → RabbitMQ → .NET processor → MongoDB
- [x] OPC-UA edge agent + simulator (on-premise telemetry path)
- [x] Alerts engine — thresholds, 3-breach rule, state machine
- [x] React dashboard — alerts, device telemetry charts, executive KPIs
- [x] Dark mode + responsive layout
- [x] Full CRUD UI for Devices / Mills / Areas + Configuration screen
- [x] Localization (i18n) — data-driven locales, server-resolved lookup
      translations, DB-backed UI overrides, CSV import/export
- [x] CI/CD — GitHub Actions build checks + Docker images published to GHCR

### Next candidates
- [ ] Reports & exports (cross-mill comparison, PDF/CSV)
- [ ] Notifications delivery (email / in-app) for alerts
- [ ] File attachments for devices (Azure Blob / MinIO)
- [ ] User management (role assignment, AD group mapping)
- [ ] AI alert summaries (Azure OpenAI / Ollama)
- [ ] E2E tests in CI + .NET unit/integration tests
- [ ] Deployment from GHCR (Azure Container Apps / Kubernetes)

> Longer-term product epics (Predictive Maintenance, Digital Twin, Energy/ESG,
> Mobile, API Marketplace, etc.) are tracked in `PRODUCT-ROADMAP.md` and the
> GitHub EPIC issues.

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
