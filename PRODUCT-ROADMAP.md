# EdgePulse -- Product Roadmap

**Version:** 2.1 — annotated with v1.0.0 delivery status
**Last Updated:** August 2026

> **Reading this after v1.0.0:** the phase/sprint numbers below are the *original
> plan* (May 2026). Actual delivery followed a different sprint sequence — each
> phase now carries a **Delivery status** line mapping its items to what shipped
> in v1.0.0 (2026-07-24) and what is deliberately post-v1.0. Authoritative
> per-sprint record: `docs/sprint-history.md` and `docs/sprints/`.
**Author:** Rakshith N S

---

## Table of Contents

1. [Product Vision](#1-product-vision)
2. [Market Analysis](#2-market-analysis)
3. [Competitive Advantage](#3-competitive-advantage)
4. [Target Customers](#4-target-customers)
5. [Pricing Strategy](#5-pricing-strategy)
6. [Phase 1 -- Foundation (Sprints 1-10)](#6-phase-1----foundation-sprints-1-10)
7. [Phase 2 -- Operations (Sprints 11-13)](#7-phase-2----operations-sprints-11-13)
8. [Phase 3 -- Intelligence (Sprints 14-16)](#8-phase-3----intelligence-sprints-14-16)
9. [Phase 4 -- Scale (Sprints 17-19)](#9-phase-4----scale-sprints-17-19)
10. [Phase 5 -- Platform (Sprints 20-22)](#10-phase-5----platform-sprints-20-22)
11. [Go To Market Strategy](#11-go-to-market-strategy)
12. [Revenue Model](#12-revenue-model)
13. [Commercialisation Path](#13-commercialisation-path)

---

## 1. Product Vision

```
EdgePulse is the affordable, configurable, on-premise-capable
industrial IoT platform for mid-market manufacturers that the
big vendors ignore because they are too busy selling €500,000
solutions to Fortune 500 companies.

We make enterprise-grade industrial monitoring accessible to
facilities with 50-500 employees, one to five factories, and
no dedicated IoT team.

We work without internet. We speak your language. We fit your
budget. We understand your industry.
```

---

## 2. Market Analysis

### The Problem With Existing Solutions

```
ENTERPRISE PLATFORMS (too expensive):
  ABB Ability          -> €100,000 - €500,000 / year
  Siemens MindSphere   -> €150,000 - €600,000 / year
  Honeywell Forge      -> €200,000+ / year
  GE Predix            -> Struggling, poor support
  PTC ThingWorx        -> Complex, expensive consultants

GENERIC IoT PLATFORMS (too generic):
  AWS IoT Greengrass   -> No industrial workflows
  Azure IoT Hub        -> Requires significant custom dev
  Google Cloud IoT     -> Discontinued in 2023

PROBLEMS THEY ALL SHARE:
  -> Cloud-only (no internet = no product)
  -> Generic (no industry-specific workflows)
  -> Expensive (SME manufacturers cannot afford)
  -> Complex (require 6-12 month implementation)
  -> Fixed (cannot customise vocabulary / workflows)
  -> Vendor lock-in (your data is hostage)
```

### The Opportunity

```
TARGET MARKET SIZE:
  Europe alone has 200,000+ mid-market manufacturers
  Pulp & Paper: 800+ mills in Europe
  Automotive suppliers: 5,000+ in Germany alone
  Food & Beverage: 30,000+ facilities in Europe

  If only 1% adopt EdgePulse at €1,000/month:
  2,000 customers x €1,000 x 12 months = €24M ARR

UNDERSERVED SEGMENT:
  Companies too big for spreadsheets
  Too small for Siemens MindSphere
  100 - 500 employees
  1 - 5 factories
  This segment has no good option today
```

---

## 3. Competitive Advantage

### The Five Differentiators

```
1. ON-PREMISE FIRST
   Works without internet.
   Data never leaves your building.
   Same features cloud or on-premise.
   No competitor offers this at our price point.

2. FULLY CONFIGURABLE
   Every dropdown, every category, every threshold
   is configurable from the UI.
   No developer needed to add a device type.
   Industry templates + tenant customisation.

3. INDUSTRY SPECIFIC
   Built for real industrial workflows.
   Pulp & Paper template out of the box.
   Manufacturing template out of the box.
   Vocabulary matches your facility -- not generic IoT terms.

4. AFFORDABLE
   €500 - €2,000 per month per facility.
   €15,000 one-time on-premise license.
   10x - 100x cheaper than enterprise platforms.
   SME manufacturers can actually afford this.

5. DOMAIN EXPERTISE
   Built by someone who spent 10 years at ABB
   building industrial MES systems.
   Understands OPC-UA, SCADA, PLC integration.
   Understands how paper mills and factories actually work.
   Not a generic software company guessing at requirements.
```

---

## 4. Target Customers

### Primary Target

```
WHO:
  Mid-market industrial manufacturers
  50 - 500 employees
  1 - 5 factories
  Annual revenue: €10M - €200M

WHERE:
  Finland (paper, pulp, process industry)
  Sweden (paper, automotive, manufacturing)
  Germany (automotive suppliers, manufacturing)
  Norway (process industry, oil & gas adjacent)

WHAT THEY NEED:
  Real-time device monitoring
  Alert management
  Maintenance tracking
  ESG reporting (EU requirement)
  Mobile access for floor operators
  No cloud dependency (many have no internet on floor)

WHY THEY WILL BUY:
  One avoided machine breakdown = €50,000 saved
  EdgePulse costs €6,000/year
  ROI in first avoided breakdown
```

### Secondary Target

```
WHO:
  System integrators and automation consultants
  Who implement solutions for the above companies

WHY:
  White label EdgePulse as their own product
  Add to their service portfolio
  Recurring revenue from their customers
  No product development cost

CHANNEL:
  Partner programme (Epic 21)
  Revenue share model
```

---

## 5. Pricing Strategy

### Cloud SaaS Pricing

```
STARTER (1 mill, up to 50 devices):
  €500 / month
  All core features
  Email support
  99.5% SLA

PROFESSIONAL (1 mill, up to 200 devices):
  €1,000 / month
  All features including AI
  Priority support
  99.9% SLA

ENTERPRISE (multiple mills, unlimited devices):
  €2,000+ / month (per mill)
  Custom integrations
  Dedicated support
  Custom SLA
  On-premise option included
```

### On-Premise License

```
ONE-TIME LICENSE:
  €15,000 per facility
  Includes 1 year support

ANNUAL SUPPORT:
  €2,000 / year
  Updates, bug fixes, support

IMPLEMENTATION:
  €5,000 - €15,000 professional services
  (optional, self-service also available)
```

### Partner / White Label

```
PARTNER PRICING:
  40% discount on all tiers
  Partner pays us, resells at their price
  Minimum 5 customer commitment

REVENUE SHARE:
  20% of customer revenue to EdgePulse
  Partner keeps 80%
  No upfront cost for partner
```

---

## 6. Phase 1 -- Foundation (Sprints 1-10)

**Goal:** Complete, working platform end to end
**Delivery status: ✅ SHIPPED in v1.0.0** — every item below except the AI
summaries (deferred to the AI epic, #9). Alerts also gained webhooks + auto
work orders; reports gained MTTA/MTTR; the "Polish" sprint became a full
documentation suite plus a v1.0.1 hardening pass.

```
Sprint 1:  Configuration Module
           All lookup tables configurable from UI
           Industry templates + tenant customisation
           STATUS: shipped

Sprint 2:  Organisation Module
           Tenant, Mill, Area management
           Multi-tenant hierarchy

Sprint 3:  Device Management
           Device registration, API keys, attachments
           Decommission workflow

Sprint 4:  Identity & Authentication
           Keycloak integration, JWT middleware
           Azure AD SSO, on-premise LDAP
           Role-based access control

Sprint 5:  Telemetry Pipeline
           Node.js NestJS ingestion service
           RabbitMQ / Azure Service Bus
           Processor worker service
           MongoDB / Cosmos DB storage

Sprint 6:  Alerts & Notifications
           Alert threshold configuration
           Anomaly detection (3 consecutive breaches)
           Alert lifecycle management
           Email + in-app notifications
           Azure OpenAI alert summaries

Sprint 7:  Dashboard & Reports
           React 18 dashboard
           Real-time telemetry charts
           Cross-mill comparison reports
           Executive view

Sprint 8:  DevOps & CI/CD
           GitHub Actions pipeline
           Docker image registry
           Azure Container Apps deployment
           Self-hosted runner for on-premise

Sprint 9:  AI Features (Basic)
           Azure OpenAI alert summaries (cloud)
           Ollama on-premise alert summaries
           Natural language device queries

Sprint 10: Polish & Go Live
           Performance optimisation
           Security audit
           Documentation
           Demo environment with NordPulp sample data
```

---

## 7. Phase 2 -- Operations (Sprints 11-13)

**Goal:** Make EdgePulse indispensable for day-to-day operations
**Delivery status:** Predictive Maintenance → **shipped as statistical health
scoring + linear RUL indicator** (v1.0, #32); Energy & ESG → **shipped** (v1.0,
#33); Mobile App → **post-v1.0** (#31).

```
Sprint 11: Mobile App (React Native)
           iOS + Android from one codebase
           Real-time device status
           Push notifications for alerts
           Acknowledge alerts from mobile
           QR code scan -> device dashboard
           Works offline (on-premise)

           WHY NOW: Operators are on the floor, not at desks.
           Without mobile, adoption rate will be low.

Sprint 12: Predictive Maintenance
           ML model training on historical telemetry
           Remaining useful life (RUL) prediction
           Failure probability score per device
           Maintenance scheduling recommendations
           Azure ML (cloud) + ONNX (on-premise)

           WHY NOW: Highest ROI feature.
           Justifies entire product cost.
           One avoided breakdown = €50,000 saved.

Sprint 13: Energy Monitoring & ESG Reporting
           Real-time energy consumption tracking
           Carbon emissions calculation (kWh to CO2)
           ESG compliance reports (ISO 14001)
           EU taxonomy alignment reporting
           Automated report generation and delivery

           WHY NOW: EU ESG reporting mandatory from 2025.
           Every EU manufacturer needs this.
           Strong sales hook for new customers.
```

---

## 8. Phase 3 -- Intelligence (Sprints 14-16)

**Goal:** Deep integration with existing factory infrastructure
**Delivery status:** OPC-UA → **shipped with auto-discovery** (v1.0, #34;
Modbus/MQTT connectors post-v1.0); Work Orders → **shipped** (v1.0, #35);
Compliance & Audit → **audit trail + CSV evidence shipped** (v1.0, #36; custom
report builder / signatures post-v1.0).

```
Sprint 14: OPC-UA & SCADA Integration
           OPC-UA client (no Edge Agent needed)
           Auto-discover devices on OPC-UA server
           Modbus TCP support
           MQTT broker integration
           SCADA system connectors

           WHY NOW: 90% of factories already have OPC-UA.
           This removes biggest adoption barrier.
           Rakshith's core ABB expertise.

Sprint 15: Maintenance Work Orders
           Auto-create work order from alert
           Assign to maintenance technician
           Parts and materials tracking
           Maintenance history per device
           Scheduled maintenance calendar
           Mobile work order management

           WHY NOW: Closes the loop from alert to resolution.
           Makes EdgePulse the system of record.
           Very sticky -- hard to replace once adopted.

Sprint 16: Compliance & Audit Reports
           ISO 9001, 14001, 55001 report templates
           Custom report builder
           Scheduled report generation
           Audit trail reports
           Digital signature support
           Export to PDF, Excel, Word

           WHY NOW: Regulatory compliance is mandatory.
           Automated reporting saves weeks per quarter.
           Strong selling point for regulated industries.
```

---

## 9. Phase 4 -- Scale (Sprints 17-19)

**Goal:** Remove barriers to European market expansion
**Delivery status:** Multi-language → **shipped** en/fi/sv, data-driven locales
(v1.0, #37); Digital Twin → **2D floor-plan mode shipped** (v1.0, #38; 3D
post-v1.0); Edge AI → **post-v1.0** (#39).

```
Sprint 17: Multi-Language Support
           Finnish, Swedish, German, English (Phase 1)
           Norwegian, Dutch, French, Spanish (Phase 2)
           Full UI translation (React i18next)
           Date / time / number format per locale
           Email notifications in user language
           PDF reports in user language

           WHY NOW: Language is a real sales blocker in Europe.
           Finnish mills need Finnish UI.
           German plants need German UI.

Sprint 18: Digital Twin
           3D model of mill with live telemetry overlay
           Color-coded device health in 3D
           Click device in 3D to see telemetry
           2D floor plan mode (simpler alternative)
           Historical playback in 3D

           WHY NOW: Most impressive demo feature.
           Sells itself visually in customer presentations.
           Strong differentiator in RFP situations.

Sprint 19: Edge AI (On-Premise ML)
           Ollama for alert summaries on-premise
           ONNX runtime for ML inference on-premise
           Anomaly detection on-premise
           Predictive maintenance on-premise
           No data leaves the mill network

           WHY NOW: Data sovereignty is critical.
           Unique in market -- no competitor offers this.
           Opens regulated industries (defence, pharma).
```

---

## 10. Phase 5 -- Platform (Sprints 20-22)

**Goal:** Transform EdgePulse from product to platform
**Delivery status:** API & Integration Hub → **public REST API + signed webhooks
(Slack/Teams) shipped** (v1.0, #40; marketplace/SAP connectors post-v1.0);
White Label → **branding shipped** (v1.0, #41; partner portal post-v1.0);
Commercialisation → **post-v1.0** (#42).

```
Sprint 20: API Marketplace & Integration Hub
           Public REST API (full OpenAPI docs)
           Webhook support for any event
           Pre-built integrations:
             SAP, Microsoft Teams, Slack
             Power BI, Grafana, ServiceNow
             PagerDuty, Jira
           Custom webhook builder (no-code)

           WHY NOW: Platform becomes sticky.
           SAP integration closes enterprise deals.
           Partners build on top -- more distribution.

Sprint 21: White Label & Partner Programme
           Full white label (logo, domain, colors)
           Partner portal
           Revenue share model
           Branded mobile app per partner
           Target: ABB resellers, Siemens partners,
                   industrial IT consultants

           WHY NOW: Fastest path to scale.
           Partners bring existing customer trust.
           Distribution without direct sales force.

Sprint 22: Commercialisation & Go To Market
           Product website (edgepulse.io)
           Self-service onboarding + free trial
           ROI calculator
           Automated billing (Stripe)
           Customer support portal
           GDPR / EU AI Act compliance
           Pilot customer case studies
           Hannover Messe conference presence

           WHY NOW: Product is ready to sell.
           Need commercial infrastructure to scale.
```

---

## 11. Go To Market Strategy

### Year 1 (2026) -- Build & Validate

```
Q1-Q2:
  Complete Phase 1 (Sprints 1-10)
  Use as portfolio -> land better job
  Start Phase 2 (Mobile + Predictive Maintenance)

Q3-Q4:
  Find 1-2 pilot customers (ex-colleagues, network)
  Free pilot in exchange for feedback + testimonial
  Validate pricing and feature priorities
```

### Year 2 (2027) -- First Revenue

```
Q1:
  First paying customer
  €500 - €1,000 / month
  Validate sales process

Q2-Q4:
  5-10 paying customers
  First partner conversation
  Conference presence (Hannover Messe)
  Product website live
```

### Year 3 (2028) -- Growth Decision

```
Evaluate:
  If 20+ customers -> raise funding or bootstrap growth
  If <10 customers -> pivot or continue as side business

Either way:
  Strong portfolio project
  Deep product thinking demonstrated
  Domain expertise validated
  Potential acquisition target
```

---

## 12. Revenue Model

### Unit Economics (Target)

```
CUSTOMER ACQUISITION COST (CAC):
  Direct sales: €2,000 - €5,000
  (conference, demo, sales calls)
  Partner channel: €500 - €1,000
  (partner handles sales)

AVERAGE CONTRACT VALUE (ACV):
  Cloud: €12,000 / year (€1,000/month avg)
  On-premise: €17,000 (€15k license + €2k support)

PAYBACK PERIOD:
  Direct: 3-5 months
  Partner: 1-2 months

CUSTOMER LIFETIME VALUE (LTV):
  3 year average contract = €36,000
  LTV:CAC ratio = 7:1 (target >3:1)

GROSS MARGIN:
  SaaS: 80%+ (infrastructure is mostly fixed cost)
  On-premise: 90%+ (no infrastructure cost)
```

### Revenue Milestones

```
2027 Q4: €5,000 MRR  (5 customers avg €1,000)
2028 Q4: €25,000 MRR (25 customers)
2029 Q4: €100,000 MRR (goal)

€100,000 MRR = €1.2M ARR
At 5x ARR multiple = €6M valuation
```

---

## 13. Commercialisation Path

### Realistic Timeline

```
NOW -- MID 2026:
  Build Phase 1 + 2
  Use as portfolio for job search
  Land Senior / Principal Engineer role
  Better salary = more time and resource

MID 2026 -- END 2026:
  Complete Phase 2 (Mobile + Predictive Maintenance)
  Approach 2-3 ex-colleagues or contacts at mills
  Offer free 3-month pilot
  Get real feedback from real operators

2027:
  If pilot feedback positive:
    First paying customers
    Apply for Finnish startup funding:
      Business Finland R&D grant (up to €500k)
      Startup accelerators (Slush, Maria01)
  If pilot feedback mixed:
    Iterate based on feedback
    Continue as side project alongside job

2028+:
  Decision point:
    Full-time startup (if traction)
    Acquisition target (if interested buyer)
    Side business (if stable revenue)
    Portfolio only (if career goal met)

ALL PATHS ARE VALID.
The product building is valuable regardless.
```

### Finnish Startup Ecosystem Advantages

```
BUSINESS FINLAND:
  R&D grants for innovative technology companies
  Up to 50% of R&D costs covered
  EdgePulse qualifies as deep tech industrial IoT

SLUSH (Helsinki):
  World-class startup conference
  November every year
  Industrial IoT is strong vertical

MARIA01 (Helsinki):
  Top startup campus in Nordics
  Strong network of industrial companies

FINNISH INDUSTRY NETWORK:
  Stora Enso, UPM, Metso, Valmet, Wärtsilä
  All potential customers AND investors
  Finland punches above its weight in industrial tech
```

---

## Summary -- Why This Will Work

```
DOMAIN EXPERTISE:
  10 years at ABB building exactly this type of system
  Understands the pain points from the inside
  Knows what operators actually need
  Has the network of potential customers

TECHNICAL EXECUTION:
  Clean Architecture, proven patterns
  Cloud + on-premise from day one
  Production-grade security and scalability
  Modern tech stack that attracts developers

MARKET TIMING:
  EU ESG regulations driving demand NOW
  AI making intelligent monitoring affordable
  OT/IT convergence accelerating
  Post-COVID focus on operational resilience

UNFAIR ADVANTAGE:
  Affordable (10x cheaper than alternatives)
  On-premise (no competitor at this price)
  Configurable (no hardcoded workflows)
  Industry-specific (built by domain expert)
```

---

*Document ends.*
*Review and update after each phase completion.*
*Next review: After Sprint 10 completion.*
