# Sprint 25 — OPC-UA Auto-Discovery (#34)

**Date:** July 2026
**Goal:** Remove the biggest onboarding barrier — instead of hand-writing the
agent's device/metric mapping, browse the factory's OPC-UA server and
generate it.

---

## What was delivered

- **`--discover` mode** in the OPC-UA agent (`npm run discover [-- endpoint]`,
  default `opc.tcp://localhost:4840`):
  1. connects anonymously (Security None, same posture as the agent),
  2. recursively browses the address space from ObjectsFolder
     (depth ≤ 4, ≤ 500 nodes; the server's own diagnostics tree and
     ns=0 plumbing like `Aliases` are filtered out),
  3. prints a human-readable inventory of every Object that carries
     Variable children, and
  4. emits a **ready-to-paste `devices[]` config snippet** — one device per
     Object, one metric per Variable with a snake_cased key — the operator
     registers each device in EdgePulse and fills in the ids.

## Verified end-to-end (live, against the running simulator)
```
[Discovery] Found 20 device candidate(s), 52 variable(s)
  ▪ NordPulp/LW_FeedWaterPump
      bearing_temp   ns=1;s=NordPulp.LW_FeedWaterPump.bearing_temp
      …
```
✅ All 20 NordPulp devices (10 Lakewood + 10 Riverside) with all 52
variables discovered; config snippet matches the agent schema exactly.

## Scope notes (epic #34)
- Shipped across the epic: OPC-UA edge agent + simulator (Sprint 11),
  auto-discovery (this sprint). The agent is the on-premise integration path.
- Modbus TCP, MQTT broker ingestion and vendor SCADA connectors remain
  post-v1.0 roadmap items; the REST ingestion service already provides a
  protocol-agnostic fallback for anything that can POST JSON.
