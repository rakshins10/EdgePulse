# Sprint 14 — Edit / Delete CRUD for Mills, Areas, Devices

**Status:** Done
**Commit:** `0c93bb5`
**Date completed:** 2026-06-09

---

## Goal

Complete the CRUD story by adding the missing Edit and Delete operations
(backend endpoints + UI) for Mills, Areas, and Devices, with safety guards and
an automated end-to-end test suite.

---

## What was built

### Backend (CQRS commands + handlers + endpoints)
- `UpdateMillCommand` + `DeleteMillCommand`
- `UpdateAreaCommand` + `DeleteAreaCommand`
- `UpdateDeviceCommand` (Decommission/DELETE already existed)
- `OrganisationController`: `PUT`/`DELETE` for mills and areas
- `DevicesController`: `PUT` for device edit

### Safety guards (return 409 Conflict)
- Delete mill — blocked if it has active areas or devices
- Delete area — blocked if it has active devices
- Edit device — area can change only within the same mill (to move across mills,
  decommission and re-register)

### Authorization
- Mills: SuperAdmin can edit/delete across all tenants (matching the existing
  `GetMillsQuery` pattern); CustomerAdmin tenant-scoped
- Areas/Devices: tenant-scoped; MillManager limited to their own mill

### Frontend
- MillsPage: Edit/Delete on each mill card; Edit/Delete on each area row;
  unified Add+Edit modals
- DevicesPage: Edit button alongside Decommission; mill is read-only in edit
- Delete errors surface the backend's specific reason (parsed from
  `ProblemDetails.title`)

### Testing — Playwright E2E (new)
- `playwright.config.ts` + `e2e/sprint14-crud.spec.ts`
- 4 tests: mill edit persists; mill delete blocked when it has children; device
  edit persists; Configuration Location Types CRUD loads — all green

---

## Bugs found & fixed during testing

1. **400 on string enums** — ASP.NET defaulted to integer enums; the frontend
   sent `"Cloud"`. Fixed by registering `JsonStringEnumConverter` globally.
2. **SuperAdmin couldn't edit cross-tenant mills** — Update/Delete handlers were
   stricter than `GetMillsQuery`; aligned them to the SuperAdmin-sees-all pattern.
3. **Generic error alert** — frontend read `err.response.data.message`, but
   ASP.NET `ProblemDetails` uses `.title`. Fixed so the real rule violation shows.

---

## Side effect

The duplicate-named "Lakewood Mill" (Tampere, Dev tenant) was renamed to
"Lakewood Mill (Tampere)" during testing, disambiguating it from the NordPulp
Lakewood Mill.
