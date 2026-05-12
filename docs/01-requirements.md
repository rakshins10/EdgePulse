# EdgePulse — Requirements Document

**Version:** 1.0  
**Status:** Approved ✅  
**Last Updated:** May 2026  
**Author:** Rakshith N S  

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Stakeholders](#2-stakeholders)
3. [Organizational Hierarchy](#3-organizational-hierarchy)
4. [Roles & Permissions](#4-roles--permissions)
5. [Functional Requirements](#5-functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Out of Scope](#7-out-of-scope)
8. [Open Questions](#8-open-questions)
9. [Assumptions](#9-assumptions)
10. [Glossary](#10-glossary)

---

## 1. Project Overview

### 1.1 Problem Statement

Industrial facilities such as pulp and paper mills operate hundreds of devices — pumps, motors, valves, sensors — continuously generating operational data. Currently, anomaly detection relies on manual inspection. By the time operators identify a fault, significant damage, downtime, or safety incidents may have already occurred.

### 1.2 Solution

EdgePulse is a **multi-tenant Industrial IoT Device Management Platform** that:

- Provides a central registry of all industrial devices across multiple facilities
- Continuously ingests high-frequency telemetry data from devices
- Automatically detects anomalies using configurable threshold rules
- Generates AI-powered human-readable alert summaries
- Notifies the right people at the right time based on their role and scope
- Presents real-time and historical data via a web dashboard

### 1.3 Real-World Context

**Reference Customer:** NordPulp Industries  
**Mills in scope:**
- Lakewood Mill — Lakewood, Finland
- Riverside Mill — Riverside, Sweden

Each mill has multiple operational areas (Paper Machines, Pulp Processing, Water Treatment). Each area contains multiple industrial devices.

### 1.4 Goals

| Goal | Description |
|------|-------------|
| Reduce unplanned downtime | Detect anomalies before failures occur |
| Centralize device visibility | Single dashboard across all mills and areas |
| Role-appropriate access | Each user sees and acts on only what they own |
| Audit compliance | Full audit trail of all actions and alerts |
| Scalability | Support multiple customers, each with multiple mills |

---

## 2. Stakeholders

| Stakeholder | Role in System | Example |
|-------------|---------------|---------|
| EdgePulse Platform Team | Owns and operates the platform | Platform Admin |
| Customer IT Admin | Manages their organization's instance | NordPulp Industries IT Manager |
| Mill Manager | Oversees one physical facility | Lakewood Mill Plant Manager |
| Operator / Technician | Day-to-day device operations | Floor technician on PM1 |
| Executive | Reads cross-mill reports and KPIs | NordPulp Industries CEO |

---

## 3. Organizational Hierarchy

EdgePulse uses a **4-level hierarchy** to model real industrial organizations:

```
Platform (EdgePulse)
└── Customer (Tenant)
      └── Mill (Physical Facility / Site)
            └── Area (Department / Production Line)
                  └── Device (Physical Equipment)
```

### 3.1 Hierarchy Example — NordPulp Industries

```
Customer: NordPulp Industries
│
├── Mill: Lakewood Mill (Lakewood, Finland)
│     ├── Area: Paper Machine 1
│     │     ├── PUMP-LW-001   Feed Pump
│     │     ├── MOTOR-LW-001  Drive Motor
│     │     └── SENSOR-LW-001 Temperature Sensor
│     ├── Area: Paper Machine 2
│     │     ├── PUMP-LW-002
│     │     └── VALVE-LW-001
│     └── Area: Pulp Processing
│           ├── PUMP-LW-003
│           └── SENSOR-LW-002
│
└── Mill: Riverside Mill (Riverside, Sweden)
      ├── Area: Paper Machine 1
      │     ├── PUMP-RV-001
      │     ├── MOTOR-RV-001
      │     └── SENSOR-RV-001
      ├── Area: Paper Machine 2
      │     ├── PUMP-RV-002
      │     └── SENSOR-RV-002
      └── Area: Water Treatment
            ├── PUMP-RV-003
            ├── VALVE-RV-001
            └── SENSOR-RV-003
```

### 3.2 Hierarchy Rules

- A **Customer** can have one or more **Mills**
- A **Mill** belongs to exactly one **Customer**
- A **Mill** can have one or more **Areas**
- An **Area** belongs to exactly one **Mill**
- A **Device** belongs to exactly one **Area**
- Data is strictly isolated between Customers — no cross-tenant data access

---

## 4. Roles & Permissions

### 4.1 Role Definitions

#### SuperAdmin *(Platform Level)*
- Owns the entire EdgePulse platform
- Created during system setup via Keycloak admin console
- Can create, suspend, and delete customer tenants
- Can impersonate any tenant for support purposes
- Sees all data across all customers

#### Customer Admin *(Tenant Level)*
- Manages one customer's entire EdgePulse instance
- Invited by SuperAdmin during tenant onboarding
- Can create and manage Mills within their tenant
- Invites and manages all users within their organization
- Assigns roles to users
- Sees all mills, areas, devices, and alerts within their tenant

#### Mill Manager *(Mill Level)*
- Manages one specific Mill
- Assigned by Customer Admin
- Sees all areas, devices, and alerts within their Mill only
- Cannot access other mills under the same customer
- Can configure alert thresholds for their mill
- Can acknowledge and assign alerts to Operators

#### Operator *(Area Level)*
- Assigned to one or more Areas **within a single Mill only**
- Cannot be assigned across different mills
- Sees only devices and telemetry in their assigned areas
- Can acknowledge alerts for their assigned devices
- Cannot modify device configuration or manage users

#### Executive *(Read Only — Tenant Level)*
- Read-only access scoped to the entire customer tenant
- Can view dashboards, telemetry, and cross-mill reports
- Cannot take any action — no alert acknowledgement, no config changes
- Does NOT receive alert notifications
- Suitable for: CEOs, auditors, external consultants, board members

### 4.2 Permissions Matrix

| Feature | SuperAdmin | Cust. Admin | Mill Mgr | Operator | Executive |
|---------|-----------|-------------|----------|----------|-----------|
| **PLATFORM** |
| Create / delete tenants | ✅ | ❌ | ❌ | ❌ | ❌ |
| Manage platform config | ✅ | ❌ | ❌ | ❌ | ❌ |
| View all tenants | ✅ | ❌ | ❌ | ❌ | ❌ |
| **CUSTOMER** |
| Create / delete mills | ✅ | ✅ | ❌ | ❌ | ❌ |
| Invite users | ✅ | ✅ | ❌ | ❌ | ❌ |
| Assign / change roles | ✅ | ✅ | ❌ | ❌ | ❌ |
| View all mills (own tenant) | ✅ | ✅ | ❌ | ❌ | ✅ |
| Customer-level reports | ✅ | ✅ | ❌ | ❌ | ✅ |
| **MILL** |
| Create / delete areas | ✅ | ✅ | ✅ | ❌ | ❌ |
| Register devices | ✅ | ✅ | ✅ | ❌ | ❌ |
| Edit device config | ✅ | ✅ | ✅ | ❌ | ❌ |
| Delete devices | ✅ | ✅ | ❌ | ❌ | ❌ |
| View all areas (own mill) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Mill-level reports | ✅ | ✅ | ✅ | ❌ | ✅ |
| **DEVICES & TELEMETRY** |
| View devices | ✅ | ✅ | ✅ | ✅ | ✅ |
| View telemetry | ✅ | ✅ | ✅ | ✅ | ✅ |
| Configure alert thresholds | ✅ | ✅ | ✅ | ❌ | ❌ |
| Acknowledge alerts | ✅ | ✅ | ✅ | ✅ | ❌ |
| Assign alerts to operator | ✅ | ✅ | ✅ | ❌ | ❌ |
| **AUDIT** |
| View audit logs | ✅ | ✅ | ✅ | ❌ | ❌ |
| Export data | ✅ | ✅ | ✅ | ❌ | ❌ |

### 4.3 User-to-Hierarchy Scope Examples

| User | Role | Scope |
|------|------|-------|
| platform.admin@edgepulse.com | SuperAdmin | All tenants |
| it.admin@nordpulp.com | Customer Admin | All NordPulp Industries mills |
| manager.lakewood@nordpulp.com | Mill Manager | Lakewood Mill only |
| manager.riverside@nordpulp.com | Mill Manager | Riverside Mill only |
| tech.pm1.lakewood@nordpulp.com | Operator | Lakewood → PM1 area |
| tech.riverside@nordpulp.com | Operator | Riverside → PM1 + PM2 |
| ceo@nordpulp.com | Executive | All NordPulp Industries mills (read only) |

### 4.4 Default Role for New Users

| Login Method | Default Role |
|-------------|-------------|
| Azure AD SSO (first login) | Executive (read only) |
| Email invitation by Customer Admin | Role assigned at invite time |
| Platform setup (first SuperAdmin) | SuperAdmin via Keycloak console |

---

## 5. Functional Requirements

### 5.1 Authentication & Identity

| ID | Requirement |
|----|-------------|
| AUTH-01 | System shall use Keycloak as the identity provider |
| AUTH-02 | Keycloak shall use PostgreSQL as its database |
| AUTH-03 | System shall support Azure Active Directory SSO via OIDC protocol |
| AUTH-04 | System shall support local user accounts managed within Keycloak |
| AUTH-05 | All services shall validate JWT tokens issued by Keycloak |
| AUTH-06 | JWT tokens shall contain: userId, tenantId, role, assigned mills, assigned areas |
| AUTH-07 | New Azure AD SSO users shall be assigned Executive role by default |
| AUTH-08 | Session tokens shall expire after 8 hours; refresh tokens after 7 days |
| AUTH-09 | All authentication events shall be logged in audit trail |
| AUTH-10 | System shall support Multi-Factor Authentication (MFA) via Keycloak |

### 5.2 Tenant & Organization Management

| ID | Requirement |
|----|-------------|
| TENANT-01 | SuperAdmin shall be able to create a new customer tenant |
| TENANT-02 | Each tenant shall have a unique identifier (tenantId) |
| TENANT-03 | SuperAdmin shall be able to suspend or delete a tenant |
| TENANT-04 | Customer Admin shall be able to create Mills within their tenant |
| TENANT-05 | Customer Admin shall be able to create Areas within a Mill |
| TENANT-06 | All data queries shall be automatically scoped by tenantId |
| TENANT-07 | Cross-tenant data access shall be strictly prohibited at API level |

### 5.3 Device Management

| ID | Requirement |
|----|-------------|
| DEV-01 | System shall support registering a device with: name, type, serialNumber, manufacturer, location (areaId), installationDate, status |
| DEV-02 | Device types shall include: Pump, Motor, Valve, Sensor, Compressor, Fan, Others |
| DEV-03 | System shall support updating device metadata |
| DEV-04 | System shall support decommissioning a device (soft delete) |
| DEV-05 | Each device shall have a unique deviceId per tenant |
| DEV-06 | System shall support viewing device history (telemetry, alerts, maintenance) |
| DEV-07 | System shall support searching and filtering devices by type, area, mill, status |
| DEV-08 | Device status values: Online, Offline, Maintenance, Decommissioned |

### 5.4 Telemetry Ingestion

| ID | Requirement |
|----|-------------|
| TEL-01 | System shall accept telemetry via REST API POST endpoint |
| TEL-02 | Telemetry payload shall include: deviceId, metricName, value, unit, timestamp |
| TEL-03 | Supported metrics: temperature (°C), pressure (bar), vibration (mm/s), flow rate (L/min), power consumption (kW) |
| TEL-04 | System shall handle minimum 1,000 telemetry messages per minute per tenant |
| TEL-05 | Telemetry service shall validate payload before publishing to queue |
| TEL-06 | Invalid payloads shall be rejected with clear error response |
| TEL-07 | Telemetry shall be published to Azure Service Bus queue |
| TEL-08 | Telemetry data shall be stored in Cosmos DB with deviceId as partition key |
| TEL-09 | System shall retain telemetry data for 12 months |
| TEL-10 | System shall support querying telemetry by: deviceId, metric, time range |
| TEL-11 | Each device shall be issued a unique API key upon registration |
| TEL-12 | Telemetry API shall authenticate requests using device API key in request header |
| TEL-13 | Invalid or missing API key shall result in 401 Unauthorized response |
| TEL-14 | Device API keys shall be stored in Azure Key Vault, never in plain text |

### 5.5 Anomaly Detection & Alerts

| ID | Requirement |
|----|-------------|
| ALERT-01 | System shall support configurable alert thresholds per device per metric |
| ALERT-02 | Alert severity levels: Critical, High, Medium, Low |
| ALERT-03 | Alert shall be triggered when metric value crosses threshold for **3 consecutive readings** |
| ALERT-04 | System shall generate AI-powered alert summary using Azure OpenAI |
| ALERT-05 | Alert shall contain: deviceId, metric, value, threshold, severity, timestamp, AI summary |
| ALERT-06 | Alerts shall be routed to users based on role and area scope |
| ALERT-07 | Authorized users shall be able to acknowledge an alert |
| ALERT-08 | Mill Manager shall be able to assign an alert to a specific Operator |
| ALERT-09 | Alert status values: Open, Acknowledged, Assigned, Resolved, Closed |
| ALERT-10 | All alert state changes shall be logged in audit trail |

### 5.6 Notifications

| ID | Requirement |
|----|-------------|
| NOTIF-01 | System shall support in-app notifications for all alert events |
| NOTIF-02 | System shall support email notifications for Critical and High severity alerts |
| NOTIF-03 | Users shall be able to configure their notification preferences (in-app, email, or both) |
| NOTIF-04 | Notification routing shall respect user role and area scope |
| NOTIF-05 | Executive role shall NOT receive any alert notifications |
| NOTIF-06 | Operators shall only receive notifications for alerts in their assigned areas |
| NOTIF-07 | SMS notification support is out of scope for v1 but architecture shall not prevent it |

### 5.7 Dashboard & Reporting

| ID | Requirement |
|----|-------------|
| DASH-01 | Dashboard shall show real-time device status across assigned scope |
| DASH-02 | Dashboard shall show live telemetry charts per device (last 24 hours) |
| DASH-03 | Dashboard shall show active alerts with severity indicators |
| DASH-04 | System shall support mill-level reports: device uptime, alert frequency, telemetry trends |
| DASH-05 | Customer Admin and Executive shall see cross-mill comparison reports |
| DASH-06 | Cross-mill reports shall include all metrics and support custom metric selection |
| DASH-07 | Reports shall be exportable as PDF and CSV |

### 5.8 Audit Trail

| ID | Requirement |
|----|-------------|
| AUDIT-01 | System shall log all user actions: login, logout, device changes, alert actions |
| AUDIT-02 | Audit logs shall be immutable — cannot be edited or deleted |
| AUDIT-03 | Audit logs shall contain: userId, action, entityType, entityId, timestamp, tenantId |
| AUDIT-04 | Audit logs shall be accessible to SuperAdmin, Customer Admin, and Mill Manager |
| AUDIT-05 | Audit logs shall be retained for 24 months |

---

## 6. Non-Functional Requirements

### 6.1 Performance

| ID | Requirement |
|----|-------------|
| PERF-01 | API response time shall be < 300ms for 95% of requests |
| PERF-02 | Telemetry ingestion shall handle 1,000 messages/minute per tenant |
| PERF-03 | Dashboard shall load within 2 seconds |
| PERF-04 | Alert processing (detection to notification) shall complete within 30 seconds |

### 6.2 Security

| ID | Requirement |
|----|-------------|
| SEC-01 | All communication shall use HTTPS / TLS 1.2+ |
| SEC-02 | No secrets shall be hardcoded — all secrets stored in Azure Key Vault |
| SEC-03 | Services shall use Managed Identity to access Azure Key Vault |
| SEC-04 | All API endpoints shall require valid JWT authentication |
| SEC-05 | Row-level security shall enforce tenant isolation on all database queries |
| SEC-06 | API shall implement rate limiting per tenant |

### 6.3 Availability

| ID | Requirement |
|----|-------------|
| AVAIL-01 | System shall target 99.5% uptime |
| AVAIL-02 | Telemetry queue (Service Bus) shall buffer messages during processor downtime |
| AVAIL-03 | System shall implement health check endpoints on all services |

### 6.4 Scalability

| ID | Requirement |
|----|-------------|
| SCALE-01 | System shall support onboarding new customers without code changes |
| SCALE-02 | Services shall be horizontally scalable via container replicas |
| SCALE-03 | Cosmos DB partitioning strategy shall support high-volume telemetry growth |

### 6.5 Maintainability

| ID | Requirement |
|----|-------------|
| MAINT-01 | All services shall expose structured logs consumed by App Insights |
| MAINT-02 | Distributed tracing shall be implemented across all services |
| MAINT-03 | All services shall have unit and integration test coverage > 70% |

---

## 7. Out of Scope

The following are explicitly NOT part of this version:

| Item | Reason |
|------|--------|
| Mobile application | Web dashboard is sufficient for v1 |
| Device SDK / agent | Devices send telemetry via REST API only |
| Machine Learning models | Rule-based anomaly detection only; AI used for summaries only |
| Billing / subscription management | Platform is not SaaS-commercial in v1 |
| SCADA / OPC-UA integration | Future version |
| Offline / edge processing | Cloud-only in v1 |
| Multi-language support | English only in v1 |

---

## 8. Decisions Log

All open questions have been resolved. Final decisions recorded below:

| # | Question | Decision | Rationale |
|---|----------|----------|-----------|
| OQ-01 | Executive role — keep or drop? | ✅ **Keep** as 5th role | CEOs, auditors, consultants need read-only cross-mill access |
| OQ-02 | Operators span multiple mills? | ✅ **No — one mill only** | Cleaner scope boundary, simpler permission checks |
| OQ-03 | Notification channels? | ✅ **Email + In-app** | In-app for all alerts, email for Critical and High only |
| OQ-04 | Consecutive readings to trigger alert? | ✅ **3 readings** | Balances sensitivity vs false positives |
| OQ-05 | Device API key authentication? | ✅ **Yes — unique API key per device** | Secure, traceable, revocable per device |
| OQ-06 | Cross-mill report metrics? | ✅ **All metrics + custom configurable** | Maximum flexibility for different customer needs |

---

## 9. Assumptions

- Devices can communicate over the internet and reach the telemetry API
- NordPulp Industries has an existing Azure Active Directory tenant for SSO
- All users access the system via modern web browser (Chrome, Edge, Firefox)
- Alert thresholds are configured manually — no auto-learning in v1
- One timezone per mill for telemetry display (UTC storage, local display)

---

## 10. Glossary

| Term | Definition |
|------|-----------|
| Tenant | A customer organization using EdgePulse (e.g. NordPulp Industries) |
| Mill | A physical industrial facility belonging to a tenant |
| Area | A department or production line within a Mill |
| Device | A physical piece of industrial equipment (pump, motor, sensor etc.) |
| Telemetry | Time-series sensor readings sent by a device |
| Anomaly | A telemetry reading that crosses a configured threshold |
| Alert | A system-generated notification triggered by an anomaly |
| Threshold | A configurable min/max value for a device metric |
| JWT | JSON Web Token — used for authentication between services |
| OIDC | OpenID Connect — standard protocol used by Keycloak and Azure AD |
| Tenant Isolation | Ensuring no customer can access another customer's data |
| Row-level Security | Database pattern where queries are automatically filtered by tenantId |
| SSO | Single Sign-On — one login works across all EdgePulse services |
