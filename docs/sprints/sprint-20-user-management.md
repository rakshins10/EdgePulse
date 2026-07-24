# Sprint 20 — User Management (#71)

**Date:** July 2026
**Goal:** Administer platform users from inside EdgePulse — list, create,
role assignment, enable/disable, temporary-password reset — implemented
against the Keycloak Admin REST API.

---

## What was delivered

### Backend
- **`IIdentityAdminService`** (Application) — provider-agnostic identity
  admin contract (`IdentityUser`, `CreateIdentityUser`).
- **`KeycloakAdminService`** (Infrastructure, via `AddHttpClient`) —
  master-realm `admin-cli` token (cached, refreshed 30 s early; credentials
  from `Keycloak:AdminUsername/AdminPassword`), CRUD on
  `/admin/realms/{realm}/users`. EdgePulse scoping (role / tenantId / millId /
  areaIds) lives in **user attributes**, which protocol mappers surface as
  JWT claims at login.
- Handlers (`Features/Users/`) with the authorization matrix:
  - only SuperAdmin + CustomerAdmin may manage users
  - CustomerAdmin: own tenant only, can never see/touch/mint SuperAdmins;
    created users always land in the actor's tenant
  - MillManager role requires a mill assignment (validator)
  - nobody can disable their own account
- `UsersController`: `GET /api/users`, `POST /api/users`,
  `PUT /api/users/{id}/role`, `PUT /api/users/{id}/enabled`,
  `POST /api/users/{id}/reset-password`.

### Dashboard
- **Users page** (`/users`, sidebar 👥 — visible to admins only; nav items
  now support `adminOnly`): user table (name/email, colour-coded role chip,
  active status), create-user modal (role select, mill select for
  MillManager, temporary password), change-role modal, reset-password modal,
  enable/disable with warning confirm. en/fi/sv strings.

## The Keycloak partial-PUT bug (found live)
`PUT /admin/.../users/{id}` **clears** profile fields (email, firstName,
lastName) that are absent from the representation. The first implementation
sent attribute-only bodies and silently wiped the test user's profile.
Fixed: both update paths now do read-modify-write, always sending the full
representation with only the intended fields changed. Verified: role change
and enable/disable preserve email/name/tenant.

## Verified end-to-end (live against Keycloak)
1. ✅ List: 5 realm users with roles/attributes
2. ✅ Create: 201 → user exists, temp password works
   (login yields `Account is not fully set up` = required password change)
3. ✅ Role change → 204, attributes updated, profile preserved
4. ✅ Disable/enable → 204 each
5. ✅ Reset password → 204
6. ✅ 94 unit tests green (10 new user-handler tests)

## Notes
- Production should use a dedicated service account with only
  `realm-management:manage-users` instead of the master admin password —
  documented in the auth guide (#72).
- AD/LDAP group mapping & JIT provisioning are Keycloak federation
  configuration (User Federation → LDAP; Identity Providers → OIDC/SAML with
  attribute importers) — covered in the Sprint-20 scope as documentation, not
  application code.
