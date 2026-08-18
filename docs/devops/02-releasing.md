# Releasing EdgePulse — Versioning & Release Guide

This explains how versions and releases work, and the exact steps to cut one.
It builds on [`01-cicd-guide.md`](01-cicd-guide.md).

---

## 1. The model: independent versions per component

EdgePulse is a monorepo, but each component versions **independently** — a
dashboard-only change does not force a backend bump. There are **four version
lines**, each with its own `CHANGELOG.md`:

| Component line | What it covers | Next version declared in | Release tag |
|----------------|----------------|--------------------------|-------------|
| **backend** | `edgepulse-api` + `edgepulse-telemetry-processor` | `src/backend/CHANGELOG.md` | `backend-vX.Y.Z` |
| **dashboard** | `edgepulse-dashboard` | `src/EdgePulse.Dashboard/CHANGELOG.md` | `dashboard-vX.Y.Z` |
| **ingestion** | `edgepulse-ingestion` | `src/EdgePulse.Ingestion/CHANGELOG.md` | `ingestion-vX.Y.Z` |
| **opcua-agent** | `edgepulse-opcua-agent` | `src/EdgePulse.OpcUaAgent/CHANGELOG.md` | `opcua-agent-vX.Y.Z` |

> Why are the two .NET services one line? They share the Domain / Application /
> Infrastructure libraries, so a change in those affects both — they can't be
> versioned apart. The three Node apps are independent.

**The version source is the CHANGELOG.** Each `CHANGELOG.md` has a
`## [Unreleased] — vX.Y.Z` line that declares the **next** version. CI reads that
line — it is the single source of truth. (The `package.json` / `Directory.Build.props`
version still exists for npm / MSBuild tooling; keep it roughly in sync, but CI does
not read it.)

All four components were released as **1.0.0** on 2026-07-24 and now target
**1.1.0** for ongoing development.

We use **SemVer**: `MAJOR.MINOR.PATCH` — bump PATCH for fixes, MINOR for new
backward-compatible features, MAJOR for breaking changes.

---

## 2. Two channels: beta (automatic) vs release (you cut it)

```
   merge to main (touching a component)
        │
        ▼
   BETA image published automatically for THAT component only
   version = <Unreleased target>-beta.<N>     e.g. 1.1.0-beta.3
   tags:  :1.1.0-beta.3  (immutable, the exact beta)
          :beta          (moves to the newest beta)
          :main          (moves to the newest beta — legacy alias)
          :sha-<commit>  (immutable, traceable)
        │
   you decide it's ready ──► push a version tag, e.g.  dashboard-v1.1.0
        │
        ▼
   RELEASE image for that component
   tags:  :1.1.0   :1.1   :latest
   + a GitHub Release with auto-generated notes
```

- **The beta number `N` auto-increments** — it's the count of commits touching that
  component since its last release tag (or since the changelog was introduced). No
  manual counter. It **resets to `beta.1`** automatically after each release.
- **Beta** = a rolling pre-release built on every relevant merge. Path filters mean
  only the changed component rebuilds (a dashboard PR doesn't touch backend).
- **Release** = a curated version *you* cut by pushing a tag. The maintainer
  controls when a real version exists.

**Semver ordering holds:** `1.1.0-beta.1` < `1.1.0-beta.2` < … < `1.1.0`. Betas are
pre-releases *of* their Unreleased target and correctly sort before it.

---

## 3. How to cut a release (step by step)

Example: releasing the **dashboard** as `1.1.0` (the current Unreleased target).

1. **Finalize the CHANGELOG** on a branch — in `src/EdgePulse.Dashboard/CHANGELOG.md`,
   rename the `## [Unreleased] — v1.1.0` heading to `## [1.1.0] — YYYY-MM-DD`, then
   add a fresh `## [Unreleased] — v1.2.0` (or `v1.1.1` for a patch) above it. Bump the
   `package.json` version to match if you like it in sync.
2. **Open a PR, get CI green, merge to main.** (This publishes a final *beta* of the
   about-to-be-released version.)
3. **Create and push the tag** matching the component + version:
   ```bash
   git checkout main && git pull
   git tag dashboard-v1.1.0
   git push origin dashboard-v1.1.0
   ```
   (Or on GitHub: **Releases → Draft a new release → Choose a tag → create
   `dashboard-v1.1.0`**.)
4. The **Publish — Dashboard** workflow fires on the tag and:
   - builds + pushes `ghcr.io/<owner>/edgepulse-dashboard:1.1.0`, `:1.1`, `:latest`
   - creates a **GitHub Release** named "Dashboard 1.1.0" with auto notes
5. The next merge to main starts betas at `1.2.0-beta.1` (against your new
   Unreleased target).

Same pattern for the others — just change the prefix:
`backend-v1.1.0`, `ingestion-v1.1.0`, `opcua-agent-v1.1.0`.

> **Guard:** if the `## [Unreleased]` target still equals a version that's already
> been released, CI fails the beta build with a clear message telling you to bump
> the target. This stops betas from silently regressing below a released version.

> ⚠️ **Push release tags ONE AT A TIME.** GitHub Actions does not reliably create
> a workflow run for every ref when several tags arrive in a single `git push` —
> during the 1.0.0 release, pushing all four tags together produced **zero** tag
> runs. Push them individually (a short pause between each) and confirm a run
> appears before pushing the next:
> ```bash
> git push origin backend-v1.0.0     # wait for "Publish — Backend [backend-v1.0.0]"
> git push origin dashboard-v1.0.0
> git push origin ingestion-v1.0.0
> git push origin opcua-agent-v1.0.0
> ```
> If a tag was pushed in a batch and no run fired, re-push it alone:
> `git push origin :refs/tags/<tag> && git push origin <tag>`
> (the release step is idempotent, so re-runs are safe).

---

## 4. Where artifacts live

- **Container images** → GitHub Container Registry (GHCR), under the repo's
  **Packages**. Beta and release are just different tags on the same image; the
  **Versions** tab of each package lists them all.
- **Release record + notes** → the repo's **Releases** page (one entry per tag).

Pull the newest beta, a specific beta, or a release:
```bash
docker pull ghcr.io/rakshins10/edgepulse-dashboard:beta            # newest beta
docker pull ghcr.io/rakshins10/edgepulse-dashboard:1.1.0-beta.3    # exact beta
docker pull ghcr.io/rakshins10/edgepulse-dashboard:1.0.0           # release (the shipped version)
```

---

## 5. Knowing which versions work together (optional BOM)

Because components version independently, it helps to record a known-good set
for a deployment, e.g. in a release note or a small file:

```
EdgePulse deployment 2026-07 (the v1.0.0 set — all released together)
  api / telemetry-processor : backend 1.0.0
  dashboard                 : 1.0.0
  ingestion                 : 1.0.0
  opcua-agent               : 1.0.0
```

Keep the `/api` contract backward-compatible and any recent dashboard will work
with any recent API; the BOM just makes a deploy reproducible.

---

## 6. FAQ

**Q: Where does the beta number come from?**
The workflow counts commits touching that component since its last release tag
(via the `component-version` composite action). First beta of a line is `beta.1`;
after a release it resets to `beta.1` again.

**Q: I merged a frontend-only change — did the backend rebuild?**
No. `paths:` filters mean only `Publish — Dashboard` ran.

**Q: Do betas overwrite each other?**
`:beta` and `:main` move to the newest; `:1.1.0-beta.N` and `:sha-<commit>` are
immutable, so any past beta is still pullable.

**Q: I forgot to bump the Unreleased target after releasing — what happens?**
The next beta build fails fast with a message telling you to bump it. Update the
changelog and re-merge.

**Q: Can I automate the changelog/version bump?**
Yes — later. Tools like Changesets or release-please can compute bumps from commit
messages. The manual changelog flow above needs no extra tooling and is the current
approach.
