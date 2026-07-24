# Strategy — Go-to-Market, Pricing, Competitive Analysis (Condensed)

The full narrative lives in [`PRODUCT-ROADMAP.md`](../../PRODUCT-ROADMAP.md)
(vision, market analysis, five differentiators, pricing tables, revenue
model, Finnish-ecosystem plan). This page is the operational summary and
the delivery status against it.

## Positioning

Configurable, **on-premise-first** IIoT monitoring for mid-size
manufacturers — the segment priced out of ABB Ability / Siemens MindSphere
(€100k+/yr) and underserved by generic cloud IoT. Target €500–2000/month.

## Differentiators (and their shipped proof)

| Differentiator | Shipped evidence (v1.0) |
|----------------|------------------------|
| Everything configurable, no consultants | Lookup/threshold/translation/branding all data-driven in-app |
| On-premise first, cloud optional | Full Docker stack incl. Keycloak, MailHog, HAProxy; no cloud dependency |
| Industrial out-of-the-box | OPC-UA agent **with auto-discovery**, simulator, industry templates |
| Closes the loop | Alert → notification/email/webhook → auto work order → MTTA/MTTR reporting |
| EU compliance angle | ESG energy/CO₂e reporting, audit trail with evidence export, fi/sv/en localization |

## Pricing scaffold (from the roadmap)

Cloud SaaS tiers by device count; on-premise perpetual + maintenance;
white-label partner tier (branding shipped in v1.0 — partner portal is
post-v1.0).

## Go-to-market sequence

1. **Validate** with the NordPulp-style demo (this repo is the demo).
2. **Pilot** 2–3 Finnish mills (Rakshith's network) — success metric: one
   avoided breakdown or audit-hours saved.
3. **First revenue** via annual on-prem licences; then partner channel
   (ABB/Siemens ecosystem consultants) using the white-label capability.

## Post-v1.0 commercial backlog

Mobile app (#31), 3D twin, real ML models, pre-built SAP/ServiceNow
connectors, partner portal, self-service billing — each has a shipped
v1.0 foundation to build on (webhooks, health-scoring data plumbing,
branding).
