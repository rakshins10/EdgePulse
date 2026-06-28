# Releasing EdgePulse — Versioning & Release Guide

This explains how versions and releases work, and the exact steps to cut one.
It builds on [`01-cicd-guide.md`](01-cicd-guide.md).

---

## 1. The model: independent versions per component

EdgePulse is a monorepo, but each component versions **independently** — a
dashboard-only change does not force a backend bump. There are **four version
lines**:

| Component line | What it covers | Version lives in | Release tag |
|----------------|----------------|------------------|-------------|
| **backend** | `edgepulse-api` + `edgepulse-telemetry-processor` | `src/Directory.Build.props` → `<Version>` | `backend-vX.Y.Z` |
| **dashboard** | `edgepulse-dashboard` | `src/EdgePulse.Dashboard/package.json` | `dashboard-vX.Y.Z` |
| **ingestion** | `edgepulse-ingestion` | `src/EdgePulse.Ingestion/package.json` | `ingestion-vX.Y.Z` |
| **opcua-agent** | `edgepulse-opcua-agent` | `src/EdgePulse.OpcUaAgent/package.json` | `opcua-agent-vX.Y.Z` |

> Why are the two .NET services one line? They share the Domain / Application /
> Infrastructure libraries, so a change in those affects both — they can't be
> versioned apart. The three Node apps are independent.

All four start at **0.1.0** (pre-1.0 = "feature-complete demo, not yet GA").

We use **SemVer**: `MAJOR.MINOR.PATCH` — bump PATCH for fixes, MINOR for new
backward-compatible features, MAJOR for breaking changes.

---

## 2. Two channels: beta (automatic) vs release (you cut it)

```
   merge to main (touching a component)
        │
        ▼
   BETA images published automatically for THAT component only
   tags:  :main        (moves every merge)
          :sha-<commit>(immutable, traceable)
        │
   you decide it's ready ──► push a version tag, e.g.  dashboard-v0.2.0
        │
        ▼
   RELEASE images for that component
   tags:  :0.2.0   :0.2   :latest
   + a GitHub Release with auto-generated notes
```

- **Beta** = a rolling pre-release built on every relevant merge. Path filters
  mean only the changed component rebuilds (a dashboard PR doesn't touch backend).
- **Release** = a curated version *you* cut by pushing a tag. The maintainer
  controls when a real version exists.

---

## 3. How to cut a release (step by step)

Example: releasing the **dashboard** as `0.2.0`.

1. **Bump the version in its source file** on a branch:
   - dashboard / ingestion / opcua-agent → edit `"version"` in that app's
     `package.json`
   - backend → edit `<Version>` in `src/Directory.Build.props`
2. **Open a PR, get CI green, merge to main.** (This publishes a new *beta* with
   the bumped version baked into the artifact.)
3. **Create and push the tag** matching the component + version:
   ```bash
   git checkout main && git pull
   git tag dashboard-v0.2.0
   git push origin dashboard-v0.2.0
   ```
   (Or on GitHub: **Releases → Draft a new release → Choose a tag → create
   `dashboard-v0.2.0`**.)
4. The **Publish — Dashboard** workflow fires on the tag and:
   - builds + pushes `ghcr.io/<owner>/edgepulse-dashboard:0.2.0`, `:0.2`, `:latest`
   - creates a **GitHub Release** named "Dashboard 0.2.0" with auto notes

Same pattern for the others — just change the prefix:
`backend-v0.2.0`, `ingestion-v0.2.0`, `opcua-agent-v0.2.0`.

> Keep step 1 and step 3 versions identical so the artifact's internal version
> matches its image tag and the Release.

---

## 4. Where artifacts live

- **Container images** → GitHub Container Registry (GHCR), under the repo's
  **Packages**. Beta and release are just different tags on the same image.
- **Release record + notes** → the repo's **Releases** page (one entry per tag).

Pull a specific release:
```bash
docker pull ghcr.io/rakshins10/edgepulse-dashboard:0.2.0
```

---

## 5. Knowing which versions work together (optional BOM)

Because components version independently, it helps to record a known-good set
for a deployment, e.g. in a release note or a small file:

```
EdgePulse deployment 2026-07
  api / telemetry-processor : backend 0.2.0
  dashboard                 : 0.3.1
  ingestion                 : 0.1.0
  opcua-agent               : 0.1.0
```

Keep the `/api` contract backward-compatible and any recent dashboard will work
with any recent API; the BOM just makes a deploy reproducible.

---

## 6. FAQ

**Q: I merged a frontend-only change — did the backend rebuild?**
No. `paths:` filters mean only `Publish — Dashboard` ran.

**Q: Do betas overwrite each other?**
`:main` moves to the newest; `:sha-<commit>` is immutable, so any past beta is
still pullable by its commit sha.

**Q: Can I automate the version bump?**
Yes — later. Tools like Changesets or release-please can compute bumps from
commit messages. Manual tags (above) need no extra tooling and are the current
approach.
