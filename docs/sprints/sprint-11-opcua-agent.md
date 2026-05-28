# Sprint 11 — OPC-UA Edge Agent

**Branch:** `feature/sprint-11-opcua-agent`
**Merged:** 2026-05-29
**Status:** ✅ Complete

---

## Goal

Build a production-ready OPC-UA edge agent that bridges industrial equipment
(PLCs, SCADA, DCS) to the EdgePulse telemetry pipeline. Also build a simulator
for demo/testing that generates realistic Pulp & Paper data without real hardware.

---

## Architecture

```
[OPC-UA Server / PLC]
        |
        | opc.tcp://
        |
[EdgePulse.OpcUaAgent]  <-- reads config.json (device + tag mapping)
        |
        | AMQP 0.9.1 (persistent)
        |
[RabbitMQ: telemetry.readings]
        |
[TelemetryProcessor]  <-- stores to MongoDB + evaluates alert thresholds
```

**Demo mode** (no real hardware needed):

```
[OpcUaSimulator]  --simulate flag, same binary
        |
        | opc.tcp://opcua-simulator:4840
        |
[OpcUaAgent]      CONFIG_PATH=config/config.nordpulp.json
```

---

## What Was Built

### `src/EdgePulse.OpcUaAgent/` — Node.js 20 + TypeScript

| File | What |
|------|------|
| `src/types.ts` | `TelemetryReading`, `AgentConfig`, `MetricConfig`, `DeviceConfig` interfaces |
| `src/config.ts` | Config loader with validation + env var support (`CONFIG_PATH`, `OPCUA_PORT`) |
| `src/publisher/RabbitMqPublisher.ts` | amqplib publisher, persistent messages, auto-reconnect |
| `src/opcua/OpcUaSubscriber.ts` | node-opcua client + subscriptions, latest-value snapshot, batch publish |
| `src/simulator/profiles.ts` | 20 device profiles with normal ranges + spike targets derived from DemoSeedService thresholds |
| `src/simulator/OpcUaSimulator.ts` | OPC-UA server (node-opcua), Box-Muller noise, spike state machine |
| `src/agent.ts` | Orchestrates publisher + subscriber, handles SIGINT/SIGTERM |
| `src/index.ts` | Entrypoint: `--simulate` flag → simulator mode, default → agent mode |
| `config/config.nordpulp.json` | All 20 NordPulp demo devices mapped to simulator node IDs |
| `config/config.example.json` | Template for real OPC-UA deployments |
| `Dockerfile` | Multi-stage (node:20-alpine), build + runtime |

---

### OPC-UA Subscriber — Batching Strategy

Rather than publishing on every OPC-UA value change (which would flood RabbitMQ):

1. Each monitored item stores its latest value in an in-memory `Map<deviceId, Map<metricKey, MetricReading>>`
2. A `setInterval` at `publishIntervalMs` (default 5s) snapshots the map
3. One `TelemetryReading` JSON per device is built and published to RabbitMQ

This ensures:
- The TelemetryProcessor receives predictable 5s batches, not per-change events
- Consecutive-breach counting in the alert engine works correctly
- Low message volume even with many monitored tags

---

### OPC-UA Simulator — Spike Model

Each metric profile defines:

| Field | Description |
|-------|-------------|
| `normalBase` | Centre of the healthy operating range |
| `normalNoise` | Gaussian noise std-dev (Box-Muller) |
| `spikeValue` | Target value during a spike (clearly above/below threshold) |
| `spikeIntervals` | Duration of each spike in 5s publish intervals |
| `spikeProbabilityPerMin` | Average spikes per minute |

State machine per metric:
```
NORMAL → (random trigger) → SPIKE (spikeIntervals × 5s)
       → COOLDOWN (10 intervals = 50s) → NORMAL
```

Spike intervals are set to 4 (20 seconds) by default. With `consecutiveCount = 3` in
the alert thresholds, each spike will fire exactly one alert. The 10-interval cooldown
ensures the alert engine sees the metric return to normal before the next spike.

---

### Configuration Format

```json
{
  "name": "nordpulp-opcua-agent",
  "publishIntervalMs": 5000,
  "opcua": {
    "serverUrl": "opc.tcp://opcua-simulator:4840",
    "reconnectDelayMs": 5000,
    "samplingIntervalMs": 1000
  },
  "rabbitmq": {
    "url": "amqp://edgepulse:EdgePulse@2026@rabbitmq:5672/edgepulse",
    "queue": "telemetry.readings",
    "durable": true
  },
  "devices": [
    {
      "deviceId": "40000001-0000-0000-0000-000000000001",
      "tenantId": "10000001-0000-0000-0000-000000000001",
      "millId":   "20000001-0000-0000-0000-000000000001",
      "areaId":   "30000001-0000-0000-0000-000000000001",
      "metrics": [
        { "nodeId": "ns=1;s=NordPulp.LW_FeedWaterPump.bearing_temp",
          "key": "bearing_temp", "unit": "°C" }
      ]
    }
  ]
}
```

---

### Docker Compose Services Added

```yaml
opcua-simulator:   # OPC-UA server on port 4840
  --simulate flag
  OPCUA_PORT=4840
  SIMULATOR_UPDATE_MS=2000

opcua-agent:       # Reads simulator, publishes to RabbitMQ
  depends_on: rabbitmq (healthy), opcua-simulator (healthy)
  CONFIG_PATH=/app/config/config.nordpulp.json
```

---

## How to Run (Demo)

```bash
# Start infrastructure + simulator + agent
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

# Check simulator is advertising nodes
# (use any OPC-UA client like UaExpert or opcua-commander)
# Endpoint: opc.tcp://localhost:4840

# Watch alerts appear in RabbitMQ
# http://localhost:15672 → edgepulse/telemetry.readings queue

# After ~30s, telemetry appears in MongoDB and alert engine fires
# GET /api/alerts in Swagger should show new alerts
```

```bash
# Run simulator standalone (local dev)
cd src/EdgePulse.OpcUaAgent
npm install && npm run build
npm run simulate    # OPC-UA server on :4840

# Run agent against local simulator
CONFIG_PATH=config/config.nordpulp.json npm start
```

---

## Node IDs Published

All node IDs follow the pattern: `ns=1;s=NordPulp.<DeviceBrowseName>.<metricKey>`

Example: `ns=1;s=NordPulp.LW_FeedWaterPump.bearing_temp`

The 20 device browse names match `DemoDeviceIds` constants in `DemoIds.cs`.

---

## Metrics Coverage

| Devices | Metrics |
|---------|---------|
| 10 Lakewood devices | 28 metrics |
| 10 Riverside devices | 22 metrics |
| **Total** | **20 devices, 50 metrics** |

All 21 alert threshold metric keys (`bearing_temp`, `vibration`, `pressure`, `temperature`,
`winding_temp`, `flow_rate`, `plate_gap`, `motor_temp`, `pump_temp`) are included.

---

## Next: Sprint 12 — Executive Dashboard

Build the read-only executive summary page:
- KPI tiles: total devices, open alerts, critical alerts, MTBF
- Alert trend chart (7-day bar chart, plain canvas/SVG)
- Alert distribution by severity (mini donut or bar)
- Top 5 devices by alert count
- Plain CSS Modules only
