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
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

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

> **Real-world context:** This platform is modelled after industrial operations at companies like Stora Enso, UPM, and Sappi — managing multiple paper mills across different countries, each with hundreds of industrial devices.

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
│  ┌─────────────────┐        ┌──────────────────────────────┐   │
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
│  ┌─────────────────┐        ┌──────────────────────────────┐   │
│  │ Processor Svc   │───────►│   Azure Cosmos DB            │   │
│  │ (.NET 9 Worker) │        │   (Telemetry Storage)        │   │
│  │ Anomaly detect  │        └──────────────────────────────┘   │
│  │ + OpenAI alerts │                                            │
│  └─────────────────┘                                            │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Azure Key Vault · App Insights · Container Apps         │  │
│  │  GitHub Actions CI/CD · Docker · Self-hosted Runner      │  │
│  └──────────────────────────────────────────────────────────┘  │
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
└── Customer (e.g. Stora Enso)
      └── Mill (e.g. Imatra Mill, Finland)
            └── Area (e.g. Paper Machine 1)
                  └── Device (e.g. PUMP-IM-001)
```

**Example — Stora Enso:**

```
Stora Enso
├── Imatra Mill (Imatra, Finland)
│     ├── Paper Machine 1 → PUMP-IM-001, MOTOR-IM-001, SENSOR-IM-001
│     ├── Paper Machine 2 → PUMP-IM-002, VALVE-IM-001
│     └── Pulp Processing → PUMP-IM-003, SENSOR-IM-002
│
└── Skoghall Mill (Skoghall, Sweden)
      ├── Paper Machine 1 → PUMP-SK-001, MOTOR-SK-001, SENSOR-SK-001
      ├── Paper Machine 2 → PUMP-SK-002, SENSOR-SK-002
      └── Water Treatment → PUMP-SK-003, VALVE-SK-001, SENSOR-SK-003
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

```bash
# Required
dotnet --version    # 9.0+
node --version      # 20.x+
docker --version    # 24.x+
git --version       # 2.x+
```

### Local Development Setup

```bash
# 1. Clone the repository
git clone https://github.com/rakshins10/EdgePulse.git
cd EdgePulse

# 2. Start all services with Docker Compose
docker compose up -d

# Services started:
# Keycloak      → http://localhost:8080  (admin/admin)
# PostgreSQL    → localhost:5432         (Keycloak DB)
# Device API    → http://localhost:5000  (.NET 9)
# SQL Server    → localhost:1433         (Devices DB)
# Telemetry Svc → http://localhost:3000  (Node.js)
# React App     → http://localhost:4000  (Dashboard)

# 3. Apply database migrations
cd src/EdgePulse.API
dotnet ef database update

# 4. Access the dashboard
open http://localhost:4000
```

### Environment Variables

Copy `.env.example` to `.env` and fill in your values:

```bash
cp .env.example .env
```

> ⚠️ Never commit `.env` to source control. All secrets are managed via Azure Key Vault in production.

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
| Architecture | 🔄 In Progress | System design & component architecture |
| Data Design | 📋 Planned | Database schemas & data flow |
| API Design | 📋 Planned | REST API endpoints & contracts |
| Identity Design | 📋 Planned | Keycloak config & SSO flows |
| Infrastructure | 📋 Planned | Docker, Azure, CI/CD setup |

---

## 🗺️ Roadmap

### Phase 1 — Core API & Deployment *(Months 1–2)*
- [x] Project setup & documentation
- [ ] .NET 9 Device API with Clean Architecture
- [ ] JWT authentication via Keycloak
- [ ] Docker + Docker Compose local stack
- [ ] GitHub Actions CI/CD pipeline
- [ ] Deploy to Azure Container Apps

### Phase 2 — Telemetry Pipeline *(Months 3–4)*
- [ ] Node.js Telemetry Ingestion Service
- [ ] Azure Service Bus integration
- [ ] .NET Worker Service — telemetry processor
- [ ] Cosmos DB telemetry storage
- [ ] Anomaly detection (3-reading threshold rule)

### Phase 3 — AI, Polish & Portfolio *(Months 5–6)*
- [ ] Azure Key Vault + Managed Identity
- [ ] Azure Application Insights (distributed tracing)
- [ ] Azure OpenAI alert summaries
- [ ] React Dashboard
- [ ] Cross-mill comparison reports
- [ ] Public demo deployment

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

<div align="center">
  <sub>Built with ❤️ to demonstrate real-world industrial IoT architecture</sub>
</div>
