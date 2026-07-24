# Authentication, Identity & AD/LDAP Integration

## 1. Architecture

- **Keycloak 24** is the identity provider (realm `edgepulse`).
- The **dashboard** uses the Keycloak JS adapter (OIDC auth-code flow,
  client `edgepulse-dashboard`, public).
- The **API** validates JWT bearer tokens against the realm
  (`Keycloak:Authority`, confidential client `edgepulse-api`).
- **User administration** (👥 Users page) calls the Keycloak Admin REST API
  through `KeycloakAdminService`.

## 2. Claims model

EdgePulse authorization rides on **user attributes**, surfaced as JWT
claims by protocol mappers on the `edgepulse-dashboard` client:

| Attribute → claim | Example | Used for |
|-------------------|---------|----------|
| `role` | `MillManager` | Role checks in every handler |
| `tenantId` | GUID | Multi-tenant isolation |
| `millId` | GUID | MillManager scoping |
| `areaIds` | GUID list | Operator scoping |
| `email` | user@… | Display + audit |

> If the mappers are missing, tokens lack the claims and the dashboard
> shows zeros. Add them under Client → `edgepulse-dashboard` → Client
> scopes → dedicated scope → Add mapper → *User Attribute* (multivalued for
> `areaIds`).

## 3. Roles

`SuperAdmin`, `CustomerAdmin`, `MillManager`, `Operator`, `Executive` —
semantics in the [Functionality guide](../guides/03-functionality-guide.md).
Enforcement lives in Application-layer handlers (unit-tested), including:
CustomerAdmin is tenant-locked and cannot mint SuperAdmins; nobody can
disable their own account.

## 4. User management API

`GET/POST /api/users`, `PUT /api/users/{id}/role`,
`PUT /api/users/{id}/enabled`, `POST /api/users/{id}/reset-password`.
Passwords are set **temporary** — Keycloak forces a change at first login.

Implementation note: Keycloak's `PUT /admin/.../users/{id}` **clears**
profile fields absent from the body — the service always sends the full
representation (read-modify-write).

## 5. Admin credentials

Local dev uses the master admin (`Keycloak:AdminUsername/AdminPassword` =
admin/admin). **Production:** create a service account client with only the
`realm-management: manage-users, view-users` roles and use client
credentials instead.

## 6. Azure AD SSO (OIDC)

Keycloak → Identity Providers → OpenID Connect:
1. Register an app in Entra ID; redirect URI
   `https://<keycloak>/realms/edgepulse/broker/azuread/endpoint`.
2. Configure discovery URL, client id/secret in Keycloak.
3. Add **attribute importers** mapping Entra claims/groups to the
   `role`/`tenantId`/`millId` attributes (this is the "AD group mapping").
4. First login JIT-provisions the Keycloak user with those attributes.

## 7. On-premise AD / LDAP

Keycloak → User Federation → **ldap**:
- Connection + bind DN to the domain controller, users DN, Kerberos
  optional.
- **Group-to-attribute mapping**: use the group-ldap-mapper to import AD
  groups, then a script/attribute mapper to translate group membership into
  the `role` attribute (e.g. `EP-MillManagers` → `MillManager`).
- Sync mode LDAP_ONLY keeps passwords in AD; users appear in EdgePulse's
  Users page automatically after first sync.

## 8. Token hygiene

- Access tokens are short-lived (realm default); the dashboard refreshes
  silently via the adapter.
- The API maps inbound claims without renaming (`MapInboundClaims` off).
- Sign-out clears the Keycloak session (sidebar → Sign out).
