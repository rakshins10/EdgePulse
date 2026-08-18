# Sprint 4 — Identity & Authentication

> **Dates:** May 23, 2026
> **Milestone:** Sprint 4 -- Identity & Auth
> **Status:** ✅ Complete
> **Epic:** #4 (closed)
> **PR:** #53 merged to main

---

## Goal

Wire Keycloak as the identity provider into the .NET 9 API so that:
- Every endpoint requires a valid JWT (no anonymous access)
- The real logged-in user's role, tenant, mill, and area IDs are read from the token
- Role-based access decisions (SuperAdmin-only, MillManager-scoped, etc.) use real JWT identity

---

## Stories Delivered

| # | Story | What |
|---|-------|------|
| #49 / US-020 | Configure Keycloak Realm | Keycloak 24 realm, client, roles, mappers, 5 test users |
| #50 / US-021 | JWT Bearer Middleware | `AddJwtBearer`, Swagger Authorize button, 401 on all unauthenticated requests |
| #51 / US-022 | Real CurrentUserService | Replace hardcoded dev user with real JWT claim reads |
| #52 / US-023 | [Authorize] on all controllers | All three controllers locked down |

---

## What Was Built

### Key files changed/created

| File | Change |
|------|--------|
| [src/backend/EdgePulse.API/Program.cs](../../src/backend/EdgePulse.API/Program.cs) | JWT Bearer setup, MapInboundClaims, Swagger Bearer button |
| [src/backend/EdgePulse.API/appsettings.json](../../src/backend/EdgePulse.API/appsettings.json) | Keycloak section added |
| [src/backend/EdgePulse.Infrastructure/Services/CurrentUserService.cs](../../src/backend/EdgePulse.Infrastructure/Services/CurrentUserService.cs) | Full rewrite — reads from JWT claims |
| [src/backend/EdgePulse.API/Controllers/ConfigurationController.cs](../../src/backend/EdgePulse.API/Controllers/ConfigurationController.cs) | `[Authorize]` added |
| [src/backend/EdgePulse.API/Controllers/OrganisationController.cs](../../src/backend/EdgePulse.API/Controllers/OrganisationController.cs) | `[Authorize]` added |
| [src/backend/EdgePulse.API/Controllers/DevicesController.cs](../../src/backend/EdgePulse.API/Controllers/DevicesController.cs) | `[Authorize]` added |
| [docs/keycloak-setup.md](../keycloak-setup.md) | Full manual Keycloak setup guide (Option A: import, Option B: step-by-step) |
| [infrastructure/keycloak/edgepulse-realm.json](../../infrastructure/keycloak/edgepulse-realm.json) | Realm export for fast re-setup |

### JWT configuration (Program.cs)

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/edgepulse";
        options.Audience  = "account";
        options.RequireHttpsMetadata = false; // dev only

        // CRITICAL: keeps "role" as "role" — not remapped to long WS-* URI
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });
```

### CurrentUserService (Infrastructure/Services)

```csharp
// All claims read via User?.FindFirst("claimName")?.Value
// MapInboundClaims=false means names are exactly what Keycloak sends

public string UserId  => Claim("sub")      ?? string.Empty;
public string Email   => Claim("email")    ?? string.Empty;
public Guid TenantId  { get { Guid.TryParse(Claim("tenantId"), out var id); return id; } }
public UserRole Role  { get { Enum.TryParse(Claim("role"), out UserRole r); return r; } }
public Guid? MillId   { get { return Guid.TryParse(Claim("millId"), out var id) ? id : null; } }
public IReadOnlyList<Guid> AreaIds => User.FindAll("areaIds")... // Multivalued claim
```

### Keycloak custom mappers

Four **User Attribute** mappers on the `edgepulse-api-dedicated` scope:

| Mapper name | Claim name | Type | Multivalued |
|-------------|------------|------|-------------|
| tenantId | `tenantId` | String | OFF |
| role | `role` | String | OFF |
| millId | `millId` | String | OFF |
| areaIds | `areaIds` | String | **ON** |

> ⚠️ Use **User Attribute** type, NOT "User Realm Role". See [Lessons Learned](#lessons-learned).

---

## Lessons Learned

### 1. `MapInboundClaims = true` (the default) breaks custom claim lookups

**Symptom:** `Claim("role")` returns null even though the JWT has `"role":"SuperAdmin"`.

**Cause:** By default, ASP.NET Core's JwtBearer middleware remaps JWT claim names to their
long WS-* URIs. `role` → `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`.
`Claim("role")` therefore never matches.

**Fix:** Set `options.MapInboundClaims = false`. Now all claim names stay exactly as
Keycloak sends them.

**Where:** `Program.cs` → `AddJwtBearer` options.

---

### 2. Keycloak's "User Realm Role" mapper picks the wrong role

**Symptom:** Token contains `"role":"default-roles-edgepulse"` even when `SuperAdmin` is assigned.

**Cause:** "User Realm Role" with `Multivalued: OFF` picks the first role in the user's list
alphabetically/by insertion order. `default-roles-edgepulse` is added to every user automatically
and comes first.

**Fix:** Use "User Attribute" mapper for `role`. Set `role` as an explicit user attribute
(`SuperAdmin`, `MillManager`, etc.) — you have full control over the value.

---

### 3. `VERIFY_PROFILE` blocks token requests in Keycloak 24

**Symptom:** `{"error":"invalid_grant","error_description":"Account is not fully set up"}`

**Cause:** Keycloak 24 evaluates `VERIFY_PROFILE` at login time when `firstName`/`lastName`
are missing — even if `requiredActions: []` on the user. Blocks the direct grant flow.

**Fix:** Disable `VERIFY_PROFILE` in Authentication → Required Actions.

---

### 4. Custom user attributes silently dropped (Keycloak 24)

**Symptom:** `PUT /admin/realms/edgepulse/users/{id}` returns 204 but attributes don't appear.

**Cause:** Keycloak 24 introduced `unmanagedAttributePolicy`. Default is `DISABLED` — custom
attributes not in the user profile schema are silently discarded.

**Fix:** Realm settings → User profile → Unmanaged attributes → **Enabled (all users)**.

---

### 5. `Audience` must be `"account"`, not `"edgepulse-api"`

**Symptom:** JWT validation fails with audience mismatch.

**Cause:** Keycloak sets `"aud":"account"` in access tokens by default.

**Fix:** `Keycloak:Audience = "account"` in appsettings.json.

---

## Test Verification

Run these commands to verify the sprint is working. Keycloak must be running.

### Get a token

```bash
TOKEN=$(curl -s -X POST "http://localhost:8080/realms/edgepulse/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&client_id=edgepulse-api&client_secret=<edgepulse-api-client-secret>&username=superadmin&password=Test@1234" \
  | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)
```

### Test cases

| # | Command | Expected |
|---|---------|----------|
| 1 | `curl -s -o /dev/null -w "%{http_code}" http://localhost:5104/api/devices` | **401** — no token |
| 2 | `curl ... -H "Authorization: Bearer $TOKEN" .../api/configuration/device-types` | **200** — SuperAdmin |
| 3 | `curl ... -H "Authorization: Bearer $TOKEN" .../api/organisation/tenants` | **200** — SuperAdmin allowed |
| 4 | (get operator token, same pattern) `.../api/organisation/tenants` | **403** — Operator forbidden |
| 5 | (operator token) `.../api/devices` | **200** — Operator can list devices |

### Check MediatR logs — should show real Keycloak IDs

```
EdgePulse Request: GetTenantsQuery UserId: 1fff3368-... TenantId: 00000099-...
```

---

## Reference

### Test user credentials

| Username | Password | Role | TenantId |
|----------|----------|------|----------|
| `superadmin` | `Test@1234` | SuperAdmin | `00000099-0000-0000-0000-000000000001` |
| `customeradmin` | `Test@1234` | CustomerAdmin | `00000099-0000-0000-0000-000000000001` |
| `millmanager` | `Test@1234` | MillManager | same + millId |
| `operator` | `Test@1234` | Operator | same + areaIds |
| `executive` | `Test@1234` | Executive | same |

### Keycloak IDs

| User | Keycloak sub |
|------|-------------|
| superadmin | `1fff3368-8676-4c1c-b151-afdb5f912294` |
| customeradmin | `a88989b6-fb96-4e22-9396-a0509cffef17` |
| millmanager | `08cbee96-c366-4bb3-906f-e4ab5208e5b2` |
| operator | `96c7702c-c44b-470a-9a87-b9243d8639bc` |
| executive | `e7d9706e-5418-4415-b50a-02b6007d2b72` |

Full Keycloak setup details → [docs/keycloak-setup.md](../keycloak-setup.md)

---

## What's Next — Sprint 5: Telemetry Pipeline

Sprint 5 builds the data path from physical device → API → message queue → processor → MongoDB.

**Stories planned:**
- US-024: NestJS Telemetry Service — Node.js service that accepts telemetry from devices (device API key auth)
- US-025: RabbitMQ publisher — telemetry events published to `telemetry.raw` queue
- US-026: .NET Worker Service — consumer that reads from queue, validates, persists to MongoDB
- US-027: Telemetry query API — GET endpoints on the main API for dashboards to read telemetry

The device API key (generated in Sprint 3 during registration) is used to authenticate inbound
telemetry — Keycloak JWT is NOT used here. Devices cannot do OIDC flows.
