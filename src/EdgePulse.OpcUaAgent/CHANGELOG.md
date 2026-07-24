# Changelog — OPC-UA Agent

All notable changes to `edgepulse-opcua-agent` are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/);
versions follow [SemVer](https://semver.org/).

> **How versioning works.** The version on the **`## [Unreleased]`** line below is
> the *next* version. Every merge to `main` that touches the agent publishes a
> beta image tagged `<next>-beta.<N>` (N auto-increments per merge). Cutting a
> release — pushing an `opcua-agent-v<next>` tag — publishes the stable image, then
> you rename the section below to `## [<next>] — YYYY-MM-DD` and start a fresh
> `## [Unreleased]` with the next target.

## [Unreleased] — v1.1.0

_Post-1.0 development._

## [1.0.0] — 2026-07-24

First release line.

### Added
- **Auto-discovery (Sprint 25)** — `npm run discover` browses an OPC-UA
  server and prints a ready-to-paste `devices[]` config snippet (one metric
  per variable, snake_cased keys; server-internal nodes filtered).
- Node + TypeScript **OPC-UA edge agent** — reads tags from OPC-UA servers and
  forwards readings into the EdgePulse pipeline.
- **Device simulator** for running the agent without physical hardware.
- Dockerfile and CI/CD publishing to GHCR.

### Fixed
- Resolved TypeScript compile errors that blocked the Docker build.
- Added `package-lock.json` for reproducible installs.
