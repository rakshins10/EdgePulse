# EdgePulse CI/CD — A Beginner's Guide

This document explains the EdgePulse continuous-integration / continuous-delivery
(CI/CD) setup **from zero**. It assumes no prior DevOps experience. Read it
top-to-bottom once; afterwards use the section headings as a reference.

> TL;DR of what we built:
> - **CI** (`.github/workflows/ci.yml`): every push/PR compiles the .NET backend
>   and builds the React dashboard. If it doesn't compile, you find out before merge.
> - **CD** (`.github/workflows/docker-publish.yml`): every merge to `main` builds
>   all five service images and publishes them to GitHub Container Registry (GHCR).

---

## 1. What is CI/CD, and why bother?

When you work alone on a laptop, "it builds on my machine" feels like enough.
It isn't, because:

- You might have uncommitted files the build secretly depends on.
- A teammate (or future you) clones the repo and it won't build.
- You forget to run the dashboard build and push code that doesn't compile.
- Producing the deployable artifacts (Docker images) by hand is slow and easy
  to get wrong.

**CI (Continuous Integration)** = a server automatically builds and checks your
code every time you push. It's an always-on, neutral second machine that proves
the code is healthy.

**CD (Continuous Delivery/Deployment)** = once the code is healthy, the same
automation produces the *deployable artifacts* (here: Docker images) and puts
them somewhere ready to run. "Delivery" stops at "image is published and ready";
"Deployment" would go one step further and actually run it on a server.

```
   you push code
        │
        ▼
   ┌─────────────┐     pass      ┌──────────────────────┐
   │     CI      │ ───────────►  │  CD (only on main)   │
   │ build/check │               │ build + push images  │
   └─────────────┘               └──────────────────────┘
        │ fail                            │
        ▼                                 ▼
   you get a red ✗               images live in GHCR,
   on the PR                     ready to pull & run
```

We use **GitHub Actions** — GitHub's built-in automation. It's free for this
repository and needs no extra servers.

---

## 2. GitHub Actions vocabulary (the only 7 words you need)

| Term | What it means | In our repo |
|------|---------------|-------------|
| **Workflow** | A YAML file describing automation. Lives in `.github/workflows/`. | `ci.yml`, `docker-publish.yml` |
| **Trigger** (`on:`) | The event that starts a workflow. | push to main, pull request, manual |
| **Job** | A group of steps that runs on one fresh machine. Jobs run in parallel by default. | `backend`, `frontend`, `build-and-push` |
| **Runner** | The throwaway virtual machine a job runs on. We use GitHub's free `ubuntu-latest`. | — |
| **Step** | A single command or pre-made action inside a job. | "Restore", "Build", … |
| **Action** | A reusable building block (`uses:`), e.g. "check out the code". | `actions/checkout@v4` |
| **Secret / token** | A credential available to a workflow, never printed. | the automatic `GITHUB_TOKEN` |

A workflow file is just: *"**on** these events, run these **jobs**; each job runs
**steps** on a **runner**."*

---

## 3. The CI workflow, explained

File: [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)

```yaml
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]
```
**When it runs:** on any push to `main`, and on any pull request aimed at `main`.
So if you open a PR, GitHub builds your branch and shows a ✓ or ✗ right on the PR.

```yaml
jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4            # 1. copy the repo onto the runner
      - uses: actions/setup-dotnet@v4        # 2. install the .NET 9 SDK
        with: { dotnet-version: '9.0.x' }
      - run: dotnet restore src/backend/EdgePulse.sln   # 3. download NuGet packages
      - run: dotnet build src/backend/EdgePulse.sln -c Release --no-restore  # 4. compile
```
Four steps: get the code, install the toolchain, restore dependencies, compile.
If compilation fails, the job goes red and the PR is blocked (once you enable
branch protection — see §7).

The `frontend` job is the same shape for the dashboard: check out → install
Node 20 → `npm ci` (a clean, lockfile-exact install) → `npm run build` (which is
`tsc -b && vite build`, so it both type-checks and bundles).

The two jobs run **in parallel** on two separate machines, so the whole thing
finishes in the time of the slower one.

> **Tests in CI:** the backend job runs `dotnet test` after the build — 130 xUnit
> tests across `tests/EdgePulse.Domain.Tests` (entity behaviour) and
> `tests/EdgePulse.Application.Tests` (handlers on an EF-InMemory double +
> NSubstitute). A failing test fails the job. The frontend's Playwright E2E suite
> still runs locally only, because it needs the full stack (SQL Server, MongoDB,
> RabbitMQ, Keycloak) — see the future steps in §9.

---

## 4. Docker images & multi-stage builds (the artifacts CD produces)

A **Docker image** is a self-contained, runnable package of one service plus
everything it needs. We publish five:

| Image | Built from | Base runtime |
|-------|-----------|--------------|
| `edgepulse-api` | `src/backend/EdgePulse.API/Dockerfile` | .NET ASP.NET 9 |
| `edgepulse-telemetry-processor` | `src/backend/EdgePulse.TelemetryProcessor/Dockerfile` | .NET runtime 9 |
| `edgepulse-dashboard` | `src/EdgePulse.Dashboard/Dockerfile` | nginx (serves static build) |
| `edgepulse-ingestion` | `src/EdgePulse.Ingestion/Dockerfile` | Node 20 |
| `edgepulse-opcua-agent` | `src/EdgePulse.OpcUaAgent/Dockerfile` | Node 20 |

Every Dockerfile uses a **multi-stage build**: a big "build" image does the
compiling, then only the finished output is copied into a small "runtime" image.
You get fast, reproducible builds *and* a lean final image.

```
┌──────────── build stage ────────────┐     ┌──── runtime stage ────┐
│ .NET SDK (≈800 MB): restore, publish │ ──► │ ASP.NET runtime only  │
│ produces /app/*.dll                  │     │ + the published /app  │
└──────────────────────────────────────┘     └───────────────────────┘
        thrown away after build               this is the shipped image
```

### Two gotchas we hit (so you understand the Dockerfiles)

1. **Build context for the .NET images is the repo root (`.`), not the project
   folder.** The API and TelemetryProcessor reference sibling projects (Domain,
   Application, Infrastructure), so the build needs to see all of `src/`. That's
   why the CD workflow sets `context: .` for those two.

2. **`.dockerignore` excludes `**/bin` and `**/obj`.** Those host build folders
   contain a `project.assets.json` with absolute paths to *your laptop's* NuGet
   cache. If copied into the container, `dotnet publish` fails with a confusing
   `ResolvePackageAssets task failed`. The root [`.dockerignore`](../../.dockerignore)
   keeps them out (and makes the upload smaller/faster).

### The dashboard image specifically

The dashboard is a static site after `vite build`, served by **nginx**. Nginx
also proxies `/api/...` calls to the backend. The target is the `API_URL`
environment variable (default `http://edgepulse-api:8080`), substituted into
[`nginx/default.conf.template`](../../src/EdgePulse.Dashboard/nginx/default.conf.template)
at container start. Override it when running if your API lives elsewhere:
`-e API_URL=http://my-api:8080`.

---

## 5. The CD workflows — per-component publishing

CD is split into **one workflow per component** so each versions and publishes
independently (a dashboard change doesn't rebuild the backend). They all share a
**reusable** build/push workflow.

| Workflow | Builds | Triggers on |
|----------|--------|-------------|
| `publish-backend.yml` | `api` + `telemetry-processor` | main pushes touching `.NET` paths, or `backend-v*` tags |
| `publish-dashboard.yml` | `dashboard` | main pushes touching the dashboard, or `dashboard-v*` tags |
| `publish-ingestion.yml` | `ingestion` | …`ingestion-v*` tags |
| `publish-opcua-agent.yml` | `opcua-agent` | …`opcua-agent-v*` tags |
| `_publish-image.yml` | (reusable, called by the above) | `workflow_call` only |

**Two channels** (full details in [`02-releasing.md`](02-releasing.md)):
- **Beta** — a push to `main` touching a component publishes `:main` + `:sha-<commit>`.
  The `paths:` filter means *only the changed component* rebuilds.
- **Release** — pushing a tag like `dashboard-v0.2.0` publishes `:0.2.0`, `:0.2`,
  `:latest` and creates a GitHub Release. The tag prefix selects the component.

How a component workflow decides the channel: a small `meta` job checks
`github.ref_type` — `tag` → release (version parsed from the tag), otherwise beta.
It then calls `_publish-image.yml`, which computes the tag list and runs
`docker/login-action` → `docker/build-push-action` (with `cache-from/to: type=gha`).

> The old single `docker-publish.yml` (one matrix, unified `:latest`) was
> replaced by this per-component model.

---

## 6. GitHub Container Registry (GHCR) — where the images go

GHCR is GitHub's image registry, built into your account. Published images appear
under your profile/repo as **Packages**.

- **View them:** GitHub → your profile → **Packages** tab, or the repo's right
  sidebar → **Packages**.
- **Pull one (after it's public, see below):**
  ```bash
  docker pull ghcr.io/<your-username>/edgepulse-api:latest
  ```
- **Pull a private one** (needs login with a Personal Access Token that has
  `read:packages`):
  ```bash
  echo $YOUR_TOKEN | docker login ghcr.io -u <your-username> --password-stdin
  docker pull ghcr.io/<your-username>/edgepulse-api:latest
  ```

### First-time setup: make packages public (optional but nice for a portfolio)
By default new GHCR packages are **private**. To let anyone pull them (good for a
public portfolio):
1. GitHub → your profile → **Packages** → click `edgepulse-api`.
2. **Package settings** → **Change visibility** → **Public**.
3. Repeat per image (only needed once each).

---

## 7. Day-to-day: how you'll actually use this

**Opening a pull request**
1. Create a branch, push it, open a PR into `main`.
2. On the PR you'll see **"Some checks haven't completed yet"** → then ✓ or ✗ for
   `CI / backend` and `CI / frontend`.
3. Click **Details** on a failing check to read the exact compiler error.
4. Fix, push again — the checks re-run automatically.

**Merging to main**
1. After merge, open the repo's **Actions** tab.
2. `CI` runs again on `main`, and `Docker Publish` runs and pushes the images.
3. When it's green, the new images are in GHCR.

**(Recommended) Require CI to pass before merge — branch protection**
1. Repo → **Settings** → **Branches** → **Add branch ruleset** (or "Add rule").
2. Branch name pattern: `main`.
3. Enable **Require status checks to pass before merging**, then select
   `backend` and `frontend`.
4. Save. Now a red CI blocks the merge button.

---

## 8. Reading the Actions tab & troubleshooting

- **Actions tab** lists every run. Click a run → click a job → expand a step to
  see its log. Red ✗ marks the step that failed; the error is usually the last
  ~20 lines.
- **Re-run:** a failed run has a **Re-run jobs** button (handy for flaky network
  errors).
- **Common failures:**
  | Symptom | Likely cause | Fix |
  |---------|-------------|-----|
  | `dotnet build` error | real compile error | reproduce locally with `dotnet build src/backend/EdgePulse.sln -c Release` |
  | `npm run build` error | TypeScript error | run `npm run build` in `src/EdgePulse.Dashboard` |
  | `ResolvePackageAssets task failed` in Docker | `bin/obj` leaked into context | ensure root `.dockerignore` exists |
  | `denied: permission_denied` on push to GHCR | missing `packages: write` | already set in our workflow |

---

## 9. Future steps (documented, not yet built)

- **E2E in CI:** add a workflow that spins up SQL/Mongo/RabbitMQ/Keycloak as
  service containers (or via `docker compose`), starts the API + dashboard, then
  runs Playwright. Slower but catches real regressions.
- **Integration tests:** unit tests already run in CI (130). Next tier is
  `WebApplicationFactory`-based integration tests against service containers.
- **Actual deployment (true CD):** from GHCR you can deploy by pulling the images
  on a server / Azure Container Apps / Kubernetes. That needs hosting + secrets,
  so it's intentionally out of scope here. The images being published is the
  hand-off point.

---

## 10. File map

| File | Purpose |
|------|---------|
| `.github/workflows/ci.yml` | Build the backend + dashboard on push/PR |
| `.github/workflows/_publish-image.yml` | Reusable: build + push one image to GHCR (beta/release tags) |
| `.github/workflows/publish-backend.yml` | Publish api + telemetry-processor (beta on main, release on `backend-v*`) |
| `.github/workflows/publish-dashboard.yml` | Publish dashboard (`dashboard-v*`) |
| `.github/workflows/publish-ingestion.yml` | Publish ingestion (`ingestion-v*`) |
| `.github/workflows/publish-opcua-agent.yml` | Publish opcua-agent (`opcua-agent-v*`) |
| `src/backend/Directory.Build.props` | Backend version source (`<Version>`) for all .NET projects |
| `docs/devops/02-releasing.md` | Versioning model + how to cut a release |
| `.dockerignore` (repo root) | Keep `bin/`, `obj/`, `node_modules/` out of build contexts |
| `src/backend/EdgePulse.API/Dockerfile` | API image (.NET, context = repo root) |
| `src/backend/EdgePulse.TelemetryProcessor/Dockerfile` | Worker image (.NET, context = repo root) |
| `src/EdgePulse.Dashboard/Dockerfile` | Dashboard image (Vite build → nginx) |
| `src/EdgePulse.Dashboard/nginx/default.conf.template` | nginx SPA + `/api` proxy config |
| `src/EdgePulse.Ingestion/Dockerfile` | Ingestion image (pre-existing) |
| `src/EdgePulse.OpcUaAgent/Dockerfile` | OPC-UA agent image (pre-existing) |
