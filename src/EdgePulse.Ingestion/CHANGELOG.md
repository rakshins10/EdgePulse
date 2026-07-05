# Changelog — Ingestion

All notable changes to `edgepulse-ingestion` are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/);
versions follow [SemVer](https://semver.org/).

> **How versioning works.** The version on the **`## [Unreleased]`** line below is
> the *next* version. Every merge to `main` that touches ingestion publishes a
> beta image tagged `<next>-beta.<N>` (N auto-increments per merge). Cutting a
> release — pushing an `ingestion-v<next>` tag — publishes the stable image, then you
> rename the section below to `## [<next>] — YYYY-MM-DD` and start a fresh
> `## [Unreleased]` with the next target.

## [Unreleased] — v0.1.0

First release line.

### Added
- Node + TypeScript telemetry ingestion service — accepts device telemetry over
  REST and forwards it to RabbitMQ for the Telemetry Processor.
- Health endpoints for liveness/readiness.
- Dockerfile and CI/CD publishing to GHCR.

### Fixed
- Set `rootDir` in `tsconfig.json` so the Docker build compiles cleanly.
- Corrected service port mismatches surfaced during local run-up.
