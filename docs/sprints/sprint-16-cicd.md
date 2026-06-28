# Sprint 16 — CI/CD Pipeline (GitHub Actions + GHCR)

**Status:** Done
**Commits:** `dfcb413` (pipeline + Dockerfiles + guide), `38622af` (buildx fix),
`cdff051` (ingestion tsconfig fix). Related: `0b5a591` (dashboard UX polish).
**Date completed:** 2026-06-28

---

## Goal

Add the project's first CI/CD: automatically build/verify on every push & PR, and
publish all service Docker images to a registry on merge to `main`. Documented in
detail for a DevOps newcomer.

---

## What was built

### CI — `.github/workflows/ci.yml`
- Triggers on push to `main` and PRs to `main`
- Two parallel jobs on GitHub-hosted `ubuntu-latest`:
  - **backend** — `dotnet restore` + `dotnet build EdgePulse.sln -c Release`
  - **frontend** — `npm ci` + `npm run build` (tsc + vite) for the dashboard
- (No .NET test projects exist yet, so backend only builds — noted as future work)

### CD — `.github/workflows/docker-publish.yml`
- Triggers on push to `main` + manual `workflow_dispatch`
- Matrix builds + pushes **5 images** to `ghcr.io/<owner>/edgepulse-<name>`:
  api, telemetry-processor, dashboard, ingestion, opcua-agent
- Auth via the automatic `GITHUB_TOKEN` (`permissions: packages: write`) — no
  Docker Hub account or manual secrets
- `metadata-action` tags `:latest` + `:sha-<commit>`; GitHub Actions layer cache

### New Dockerfiles (3 were missing)
- `EdgePulse.API` — multi-stage .NET 9, **build context = repo root** so the
  Domain/Application/Infrastructure project references resolve; listens on `:8080`
- `EdgePulse.TelemetryProcessor` — multi-stage .NET 9 worker
- `EdgePulse.Dashboard` — Vite build served by nginx, with
  `nginx/default.conf.template` (SPA fallback + `/api` proxy via `${API_URL}`)
- Ingestion + OpcUaAgent Dockerfiles already existed

### Supporting
- Root `.dockerignore` excludes `**/bin`, `**/obj`, `node_modules`
- `src/EdgePulse.Ingestion/tsconfig.json`: added `"rootDir": "./src"`

---

## Bugs found & fixed via the pipeline

1. **`ResolvePackageAssets task failed`** — host `bin/obj` (with absolute NuGet
   paths) leaked into the .NET build context → fixed with root `.dockerignore`.
2. **"Cache export is not supported for the docker driver"** — `cache-to:
   type=gha` needs Buildx's docker-container driver → added
   `docker/setup-buildx-action`.
3. **Ingestion `TS5011`** — a clean Docker build needed explicit `rootDir`.

---

## Verified on GitHub

- CI: backend + frontend jobs green
- CD: all 5 images published to GHCR (run on commit `cdff051`)

## Documentation

`docs/devops/01-cicd-guide.md` — a from-zero guide: CI/CD concepts, GitHub
Actions vocabulary, line-by-line workflow walkthroughs, multi-stage builds, GHCR
usage, branch protection, reading the Actions tab, troubleshooting, and
documented future steps (E2E in CI, .NET tests, real deployment).

## Future steps

- E2E in CI (spin up SQL/Mongo/RabbitMQ/Keycloak as services, run Playwright)
- .NET unit/integration test projects (backend CI currently only builds)
- Actual deployment from GHCR (Azure Container Apps / k8s) — needs hosting + secrets
- GHCR packages default to private; make public per package if desired
