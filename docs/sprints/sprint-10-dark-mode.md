# Sprint 10 — Dark Mode + Responsive Layout

**Branch:** `feature/sprint-10-dark-mode`
**Merged:** 2026-05-29
**Status:** ✅ Complete

---

## Goal

Make the EdgePulse Dashboard theme-aware (dark/light toggle) and fully responsive
on mobile without introducing any CSS library — pure CSS custom properties
and CSS Modules only.

---

## What Was Built

### 1. CSS Custom Properties — Full Theme System

`src/EdgePulse.Dashboard/src/index.css`

All colours live in CSS variables on `:root` (dark) and `[data-theme="light"]`.
No hardcoded hex values remain in any component CSS file.

**Variable categories:**

| Variable group | Dark value | Light value |
|----------------|-----------|------------|
| `--color-bg` | `#080b12` | `#f8fafc` |
| `--color-surface` | `#0f1117` | `#ffffff` |
| `--color-surface-2` | `#0a0d14` | `#f1f5f9` |
| `--color-border` | `#1e2230` | `#e2e8f0` |
| `--color-text` | `#e2e8f0` | `#0f172a` |
| `--color-text-muted` | `#64748b` | `#64748b` |
| `--color-accent` | `#3b82f6` | `#2563eb` |
| `--color-row-hover` | `#0d1117` | `#f1f5f9` |
| `--color-overlay` | `rgba(0,0,0,0.65)` | `rgba(15,23,42,0.5)` |

**Semantic colours** (unchanged in both themes):
`--color-critical`, `--color-high`, `--color-medium`, `--color-low`

---

### 2. ThemeContext

`src/EdgePulse.Dashboard/src/context/ThemeContext.tsx`

```tsx
const { theme, toggleTheme } = useTheme();
// theme: 'dark' | 'light'
// toggleTheme(): flips and persists to localStorage
```

- Reads initial value from `localStorage('edgepulse-theme')` — defaults to `'dark'`
- Applies `data-theme` attribute to `document.documentElement` on every change
- `ThemeProvider` wraps the full app in `main.tsx`

---

### 3. ThemeToggle

`src/EdgePulse.Dashboard/src/components/layout/ThemeToggle.tsx`

A 32×32px button in the topbar right side:
- Shows `☀` when in dark mode (click to go light)
- Shows `☾` when in light mode (click to go dark)
- Uses `--color-surface-2` / `--color-border` — adapts to current theme

---

### 4. Responsive Layout — Mobile Sidebar Drawer

**Breakpoint:** `< 768px` = mobile; `≥ 768px` = desktop

**Desktop:** Sidebar fixed at 220px, always visible — no change.

**Mobile:**
- Sidebar is `position: fixed; transform: translateX(-100%)` — off-screen
- Hamburger button (`☰`) appears in topbar (hidden on desktop via `display: none`)
- Tap hamburger → sidebar slides in (`transform: translateX(0)`) with 0.25s ease
- Semi-transparent overlay behind sidebar — tap to close
- Sidebar auto-closes on route change (`useLocation` effect)

---

## Files Changed

| File | Change |
|------|--------|
| `src/index.css` | CSS custom properties (dark + light themes) |
| `src/context/ThemeContext.tsx` | **NEW** — ThemeProvider + useTheme hook |
| `src/components/layout/ThemeToggle.tsx` | **NEW** — sun/moon toggle button |
| `src/components/layout/ThemeToggle.module.css` | **NEW** |
| `src/components/layout/AppLayout.tsx` | Hamburger + mobile overlay + ThemeToggle |
| `src/components/layout/AppLayout.module.css` | CSS vars + `.menuBtn` + `.mobileOverlay` |
| `src/components/layout/Sidebar.tsx` | `isOpen`/`onClose` props + route-change close |
| `src/components/layout/Sidebar.module.css` | CSS vars + mobile drawer styles |
| `src/pages/alerts/AlertsPage.module.css` | All hex → CSS vars + responsive summary cards |
| `src/components/alerts/AlertActionModal.module.css` | All hex → CSS vars |
| `src/main.tsx` | Wrap with `ThemeProvider` |

---

## How to Test

### Dark ↔ Light toggle
1. Run `npm run dev` in `src/EdgePulse.Dashboard`
2. Log in via Keycloak
3. Click `☀` button (top-right of topbar) — page switches to light theme instantly
4. Refresh — preference persists (localStorage)
5. Click `☾` to return to dark

### Mobile responsive
1. Open Chrome DevTools → toggle device toolbar → set to 375px width
2. Sidebar should be hidden; hamburger `☰` visible in topbar
3. Tap `☰` → sidebar slides in from left with overlay behind it
4. Tap a nav link → sidebar closes, navigates
5. Tap overlay → sidebar closes

---

## Design Decisions

- **`data-theme` on `<html>`**: CSS variables cascade down from the root, no need to
  wrap components individually. Any future component just uses `var(--color-*)`.
- **`transition: background 0.2s, color 0.2s` on `body`**: smooth theme switch
  without JavaScript animation code.
- **Semantic colours unchanged**: critical/high/medium/low/accent pill colours are
  intentional brand colours — they don't flip in light mode.
- **CSS Modules only**: zero Tailwind, zero CSS framework. All styles are scoped
  and type-safe via `vite-env.d.ts`.

---

## Next: Sprint 11 — OPC-UA Edge Agent

Build the OPC-UA to RabbitMQ bridge:
- Node.js 20 + `node-opcua` package
- Reads metrics from OPC-UA server (or simulator)
- Publishes to RabbitMQ `telemetry.readings` queue
- Docker container, configurable tag-to-metric mapping
