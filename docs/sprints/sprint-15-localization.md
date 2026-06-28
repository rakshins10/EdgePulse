# Sprint 15 — Localization (i18n)

**Status:** Done (3 phases)
**Commits:** `be62575` (Phase 1), `5512a86` (Phase 2A), `2a9ec47` (Phase 2B)
**Date completed:** 2026-06-13

---

## Goal

Make every piece of displayed text configurable and translatable — no hardcoded
strings — and make the set of languages itself data-driven so new languages can
be added at runtime. Closes the platform's "Multi-Language Support" epic.

---

## Phase 1 — i18n infrastructure (`be62575`)

- `i18next` + `react-i18next` + browser language detector
- Bundled locale files: `en`, `fi` (Suomi), `sv` (Svenska) under
  `src/i18n/locales/`
- `LanguageSwitcher` in the topbar; choice persisted to `localStorage`
- Every UI string across Sidebar, Dashboard, Alerts, Devices, Mills, Areas,
  Configuration, and the time-range toolbar migrated off hardcoded text
- Pluralization via `_one`/`_other`, interpolation via `{{var}}`

## Phase 2A — Data-driven locales + lookup translations (`5512a86`)

- **`Locale` table** (code, displayName, nativeName, flag, isEnabled, isDefault,
  sortOrder), seeded en/fi/sv via EF `HasData`. CRUD in Configuration → Languages.
- **`LookupTranslation` table** — translates lookup item *names* (Pump, Floor,
  Online…) per locale, tenant-scoped.
- **Server-side name resolution** via `Accept-Language` header in the lookup
  query handlers (`ILocaleContext` + `ILookupTranslator`), falling back to the
  stored English name.
- LanguageSwitcher now fetches the enabled-locale list from the API.
- Configuration → Translations tab: pick lookup type + locale, inline-edit,
  autosave on blur.

## Phase 2B — DB-backed UI strings + CSV round-trip (`2a9ec47`)

- **`UiStringTranslation` table** — DB overrides for UI chrome keys, layered on
  top of the bundled JSON at runtime (`i18n.addResource` on load/language change).
  This lets a *newly added* language translate the chrome too, not just lookups.
- **CSV export/import** (Configuration → Languages → Import/Export): one file
  with `Category,Key,Source(English),Translation` covering both UI keys and
  lookup items. The customer workflow: export → fill the Translation column →
  import. `translationTools.ts` handles flatten + RFC-4180 CSV build/parse.
- **Pre-fill new languages from English** (checkbox on Add Language): copies all
  UI keys + lookup names as an editable starting point.
- `supportedLngs` restriction removed so runtime-added locales are selectable.

---

## How the pieces fit

| Translatable content | Stored in | Resolved |
|----------------------|-----------|----------|
| UI chrome (buttons, tabs, labels) | `en/fi/sv.json` (source) + `UiStringTranslation` (overrides) | i18next, client-side, English fallback |
| Lookup item names (Pump, Floor…) | `LookupTranslation` | server-side via `Accept-Language`, English fallback |
| Locales themselves | `Locale` table | API → LanguageSwitcher |

The frontend sends `Accept-Language` = active UI language on every axios request;
a language change invalidates React Query caches so server-resolved names refetch.

---

## Testing

- `e2e/sprint15-i18n.spec.ts` (switcher language change; Languages tab lists
  locales; Finnish translation persists) and `e2e/sprint15b-csv.spec.ts`
  (add language with pre-fill, export CSV, appears in switcher). Full suite 8/8.

## Known limitation

A brand-new language (e.g. German) gets translatable lookup names immediately,
but its UI chrome falls back to English until translations are imported (that's
exactly what the CSV round-trip is for).
