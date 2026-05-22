# EdgePulse — Keycloak Setup Guide

> Complete reference for configuring the EdgePulse identity provider.
> Last updated: May 2026 — Sprint 4 (US-020)
>
> **Two ways to set up:**
> - [Option A — Import (fast)](#option-a--import-from-saved-config) — 2 minutes, recommended for re-setup
> - [Option B — Manual (full understanding)](#option-b--manual-step-by-step) — 15-20 minutes, do this at least once

---

## Prerequisites

Start Keycloak and its database:

```bash
cd C:\Studies\EdgePulse-Application\EdgePulse
docker compose -f infrastructure/docker-compose.onpremise.yml up -d postgres keycloak
```

Wait ~30 seconds, then open **http://localhost:8080**

| What               | Value                           |
|--------------------|---------------------------------|
| Admin console      | http://localhost:8080           |
| Admin username     | `admin`                         |
| Admin password     | `admin`                         |

---

## Option A — Import from Saved Config

The realm configuration is saved in the repo. This imports the realm, client, roles, mappers
and user profile settings in one step. Users must be created manually afterwards (passwords
are never exported for security reasons).

### Step 1 — Import the realm

1. Open **http://localhost:8080** → Administration Console → log in
2. Top-left dropdown → **Create Realm**
3. Click **Browse** → select `infrastructure/keycloak/edgepulse-realm.json`
4. Realm name auto-fills as `edgepulse` → click **Create**

### Step 2 — Create test users

After import, the realm has all roles and mappers but no users. Create 5 test users by
following [Step 7 — Create test users](#step-7--create-test-users) from Option B below.

> The import already handles: realm settings, client config, roles, protocol mappers,
> user profile (unmanaged attributes enabled), VERIFY_PROFILE disabled.
> You only need to create users.

---

## Option B — Manual Step-by-Step

### Step 1 — Create the Realm

1. Open **http://localhost:8080** → Administration Console
2. Log in: `admin` / `admin`
3. Top-left dropdown (shows "Keycloak") → **Create Realm**
4. Fill in:
   - **Realm name:** `edgepulse`
   - **Enabled:** ON
5. Click **Create**

You are now inside the `edgepulse` realm. Every remaining step must be done inside this realm.
Check the top-left always shows `edgepulse`, not `master`.

---

### Step 2 — Create the API Client

1. Left sidebar → **Clients** → **Create client**

**Page 1 — General settings:**
| Field | Value |
|-------|-------|
| Client type | `OpenID Connect` |
| Client ID | `edgepulse-api` |
| Name | `EdgePulse API` |

Click **Next**.

**Page 2 — Capability config:**
| Field | Value |
|-------|-------|
| Client authentication | **ON** |
| Authorization | OFF |
| Standard flow | ✅ ticked |
| Direct access grants | ✅ ticked |

Click **Next**.

**Page 3 — Login settings:**
| Field | Value |
|-------|-------|
| Valid redirect URIs | `http://localhost:5104/*` |
| Web origins | `http://localhost:5104` |

Click **Save**.

---

### Step 3 — Copy the Client Secret

1. Still on `edgepulse-api` → click the **Credentials** tab
2. Copy the **Client secret** value and save it — you will need it in `appsettings.json`

> The current dev secret is: `lnBQYXdQnQTku1jT64LbEMyaRFRws3HS`
> ⚠️ Rotate this before any non-local deployment.

---

### Step 4 — Create Realm Roles

1. Left sidebar → **Realm roles** → **Create role** (repeat 5 times)

| Role name (exact — case-sensitive) |
|------------------------------------|
| `SuperAdmin` |
| `CustomerAdmin` |
| `MillManager` |
| `Operator` |
| `Executive` |

For each: enter the name → click **Save**.

---

### Step 5 — Add Protocol Mappers

These mappers put `tenantId`, `role`, `millId`, and `areaIds` into the JWT.

1. Left sidebar → **Clients** → click **`edgepulse-api`**
2. Click the **Client scopes** tab
3. Click the link **`edgepulse-api-dedicated`** (the first row)
4. Click **Add mapper** → **By configuration** → **User Attribute**

Create these 4 mappers one at a time:

#### Mapper 1 — tenantId

| Field | Value |
|-------|-------|
| Name | `tenantId` |
| User Attribute | `tenantId` |
| Token Claim Name | `tenantId` |
| Claim JSON Type | `String` |
| Add to ID token | ON |
| Add to access token | ON |
| Add to userinfo | ON |
| Multivalued | OFF |

Click **Save**.

#### Mapper 2 — millId

| Field | Value |
|-------|-------|
| Name | `millId` |
| User Attribute | `millId` |
| Token Claim Name | `millId` |
| Claim JSON Type | `String` |
| Add to ID token | ON |
| Add to access token | ON |
| Add to userinfo | ON |
| Multivalued | OFF |

Click **Save**.

#### Mapper 3 — areaIds

| Field | Value |
|-------|-------|
| Name | `areaIds` |
| User Attribute | `areaIds` |
| Token Claim Name | `areaIds` |
| Claim JSON Type | `String` |
| Add to ID token | ON |
| Add to access token | ON |
| Add to userinfo | ON |
| **Multivalued** | **ON** |

Click **Save**.

#### Mapper 4 — role

> ⚠️ **Important:** Use **User Attribute** type (same as above), NOT "User Realm Role".
> The "User Realm Role" mapper picks Keycloak's built-in `default-roles-edgepulse`
> instead of the assigned role. See [Lessons Learned](#lessons-learned) for details.

| Field | Value |
|-------|-------|
| Name | `role` |
| User Attribute | `role` |
| Token Claim Name | `role` |
| Claim JSON Type | `String` |
| Add to ID token | ON |
| Add to access token | ON |
| Add to userinfo | ON |
| Multivalued | OFF |

Click **Save**.

---

### Step 6 — Two Realm-Level Fixes (Keycloak 24 specific)

These two settings are not obvious in the UI but are required.

#### Fix A — Disable VERIFY_PROFILE

Without this, any user without `firstName`/`lastName` gets blocked at login with
`"Account is not fully set up"` — even with `requiredActions: []`.

1. Left sidebar → **Authentication** → **Required actions** tab
2. Find **Verify Profile** → click the row
3. Set **Enabled:** OFF → **Save**

#### Fix B — Enable Unmanaged Attributes

Without this, custom user attributes (`tenantId`, `role`, etc.) are silently dropped
when set via the Admin API — the call returns HTTP 204 but nothing is saved.

1. Left sidebar → **Realm settings** → **User profile** tab
2. Scroll to the bottom → find **Unmanaged attributes**
3. Set to **Enabled (all users)** → **Save**

---

### Step 7 — Create Test Users

Create 5 users. For each user the steps are the same:

#### Creating a user (repeat for all 5)

1. Left sidebar → **Users** → **Add user**
2. Fill in **Username** and **Email**, set **Email verified: ON** → **Create**
3. **Credentials** tab → **Set password** → enter `Test@1234` → set **Temporary: OFF** → **Save**
4. **Role mapping** tab → **Assign role** → find and select the role → **Assign**
5. **Details** tab → scroll to bottom → **Attributes** section:
   - Click **Add attribute** → enter Key and Value → **Save**

> If the Attributes section is not visible, scroll past the Save/Revert buttons.

#### User 1 — superadmin

| Field | Value |
|-------|-------|
| Username | `superadmin` |
| Email | `superadmin@edgepulse.com` |
| Role | `SuperAdmin` |
| Attribute: `tenantId` | `00000099-0000-0000-0000-000000000001` |
| Attribute: `role` | `SuperAdmin` |

#### User 2 — customeradmin

| Field | Value |
|-------|-------|
| Username | `customeradmin` |
| Email | `customeradmin@edgepulse.com` |
| Role | `CustomerAdmin` |
| Attribute: `tenantId` | `00000099-0000-0000-0000-000000000001` |
| Attribute: `role` | `CustomerAdmin` |

#### User 3 — millmanager

| Field | Value |
|-------|-------|
| Username | `millmanager` |
| Email | `millmanager@edgepulse.com` |
| Role | `MillManager` |
| Attribute: `tenantId` | `00000099-0000-0000-0000-000000000001` |
| Attribute: `role` | `MillManager` |
| Attribute: `millId` | `7de9e5a5-3ab1-48e4-ad23-2e5193c9b296` |

#### User 4 — operator

| Field | Value |
|-------|-------|
| Username | `operator` |
| Email | `operator@edgepulse.com` |
| Role | `Operator` |
| Attribute: `tenantId` | `00000099-0000-0000-0000-000000000001` |
| Attribute: `role` | `Operator` |
| Attribute: `areaIds` | `42ccc0bb-01e9-4aa6-9fcc-09408ee97663` |

#### User 5 — executive

| Field | Value |
|-------|-------|
| Username | `executive` |
| Email | `executive@edgepulse.com` |
| Role | `Executive` |
| Attribute: `tenantId` | `00000099-0000-0000-0000-000000000001` |
| Attribute: `role` | `Executive` |

---

### Step 8 — Verify

Get a token and verify the claims. Run this in Git Bash:

```bash
curl -s -X POST "http://localhost:8080/realms/edgepulse/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password" \
  -d "client_id=edgepulse-api" \
  -d "client_secret=<YOUR_CLIENT_SECRET>" \
  -d "username=superadmin" \
  -d "password=Test@1234"
```

Copy the `access_token` value and paste it into **https://jwt.io**

The decoded payload must contain:

```json
{
  "sub": "...",
  "email": "superadmin@edgepulse.com",
  "tenantId": "00000099-0000-0000-0000-000000000001",
  "role": "SuperAdmin"
}
```

Also verify `millmanager` contains `millId` and `operator` contains `areaIds`.

---

## Reference

### Database IDs used in user attributes

| Name             | ID                                      |
|------------------|-----------------------------------------|
| Dev Tenant       | `00000099-0000-0000-0000-000000000001` |
| Lakewood Mill    | `7de9e5a5-3ab1-48e4-ad23-2e5193c9b296` |
| Paper Machine 1  | `42ccc0bb-01e9-4aa6-9fcc-09408ee97663` |

### Keycloak user IDs

| Username        | Keycloak User ID                        |
|-----------------|-----------------------------------------|
| `superadmin`    | `1fff3368-8676-4c1c-b151-afdb5f912294` |
| `customeradmin` | `a88989b6-fb96-4e22-9396-a0509cffef17` |
| `millmanager`   | `08cbee96-c366-4bb3-906f-e4ab5208e5b2` |
| `operator`      | `96c7702c-c44b-470a-9a87-b9243d8639bc` |
| `executive`     | `e7d9706e-5418-4415-b50a-02b6007d2b72` |

### appsettings.json values (for US-021)

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/edgepulse",
  "Audience":  "account",
  "ClientId":  "edgepulse-api",
  "ClientSecret": "<from Credentials tab>"
}
```

> `Audience` is `account` — not `edgepulse-api`. Keycloak sets `"aud":"account"` by default
> in access tokens. Verified from decoded token.

---

## Lessons Learned

### 1. "Account is not fully set up" on token request

**Symptom:** `{"error":"invalid_grant","error_description":"Account is not fully set up"}`

**Cause:** Keycloak 24 evaluates the `VERIFY_PROFILE` action at login time when
`firstName`/`lastName` are missing — even if `requiredActions: []` on the user.
This blocks the direct grant flow because it can't show a form to the user.

**Fix:** Disable `VERIFY_PROFILE` in Authentication → Required Actions (Step 6A above).

---

### 2. Custom attributes silently dropped (tenantId not in token)

**Symptom:** `PUT /admin/realms/edgepulse/users/{id}` returns HTTP 204 but attributes
are not saved. `GET /admin/realms/edgepulse/users/{id}` shows no attributes.

**Cause:** Keycloak 24 introduced `unmanagedAttributePolicy`. The default is `DISABLED`,
meaning attributes not defined in the user profile schema are silently discarded.

**Fix:** Set `unmanagedAttributePolicy: ENABLED` in Realm settings → User profile (Step 6B).

---

### 3. `role` claim shows `default-roles-edgepulse` instead of assigned role

**Symptom:** Token contains `"role":"default-roles-edgepulse"` even when `SuperAdmin` is assigned.

**Cause:** "User Realm Role" mapper with Multivalued:OFF picks the alphabetically/insertion-ordered
first role in the user's list. `default-roles-edgepulse` is assigned to all Keycloak users
automatically and appears first.

**Fix:** Use "User Attribute" mapper for `role` (Step 5 — Mapper 4). Set `role` as an explicit
user attribute (`"SuperAdmin"`, `"MillManager"`, etc.) to have full control over the claim value.

---

### 4. "Single Role" option missing from User Realm Role mapper UI

**Symptom:** The UI in the step-by-step guide says to set "Single Role: ON" but the option
doesn't exist in Keycloak 24.

**Cause:** Removed in Keycloak 24.

**Note:** Irrelevant — use User Attribute mapper instead (see #3).
