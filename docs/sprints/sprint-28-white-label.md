# Sprint 28 — White-Label Branding (#41)

**Date:** July 2026
**Goal:** The white-label core of the Partner Programme epic — per-tenant
product name, logo and accent colour applied across the whole dashboard.

---

## What was delivered

### Backend
- `TenantBranding` entity (one row per tenant, unique index): product name
  (≤60), logo URL, accent colour (#hex validated). Migration
  `Sprint28_TenantBranding`.
- `GET /api/branding` — any authenticated user (the shell needs it at load);
  returns EdgePulse defaults when unset.
- `PUT /api/branding` — SuperAdmin/CustomerAdmin upsert.

### Dashboard
- **Configuration → Branding tab** (product name, logo URL, optional accent
  colour with a use/default toggle).
- **Live application in the shell**: `AppLayout` sets `document.title` and
  overrides the `--color-accent` / `--color-accent-hover` /
  `--color-nav-active-border` CSS variables (both themes); the sidebar shows
  the tenant's product name (and derives the collapsed two-letter mark).
  Changes apply immediately on save — no reload.

## Verified end-to-end (live)
1. ✅ Default: `{"productName":"EdgePulse", …}`
2. ✅ PUT "NordPulp Monitor" + `#0ea5e9` → 204 → GET round-trips
3. ✅ Operator: GET 200 (shell must read), PUT **403**
4. ✅ Defaults restored for the demo; single row upserted (no duplicates)
5. ✅ 130 unit tests green (3 new)

## Scope notes (epic #41)
- Shipped: full in-product white-labelling (name, logo, accent).
- Partner portal, revenue-share tooling and per-partner mobile builds are
  commercial post-v1.0 items (see the strategy doc, #79).
