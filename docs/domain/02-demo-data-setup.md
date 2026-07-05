# Demo Data Setup — NordPulp Industries

**Sprint:** 9
**Status:** Seeded and verified ✅

This document covers the complete NordPulp Industries demo environment:
what was seeded, how to re-seed, and how to drive a live demo with real alerts.

---

## What's in the Demo

| Entity | Count | Notes |
|--------|-------|-------|
| Tenant | 1 | NordPulp Industries |
| Mills  | 2 | Lakewood Mill (Finland), Riverside Mill (Sweden) |
| Areas  | 8 | 4 per mill |
| Devices | 20 | 10 per mill, real Pulp & Paper device types |
| Alert Thresholds | 21 | Real-world values from ISO 10816, industry practice |

---

## How to Seed

### First time / reset to clean state

```bash
cd EdgePulse
dotnet run --project src/backend/EdgePulse.API -- --seed
```

**The seed is idempotent** — safe to run multiple times.
If a `nordpulp` tenant already exists with a different ID (from a manual API call),
the seed removes it and recreates everything with the fixed demo IDs.

---

## Fixed GUIDs Reference

All IDs are deterministic so demo scripts always work without discovery calls.

### Tenant

| Name | ID |
|------|----|
| NordPulp Industries | `10000001-0000-0000-0000-000000000001` |

### Mills

| Name | ID | Location |
|------|----|----------|
| Lakewood Mill | `20000001-0000-0000-0000-000000000001` | Lakewood, Finland |
| Riverside Mill | `20000001-0000-0000-0000-000000000002` | Riverside, Sweden |

### Areas

| Mill | Area | ID |
|------|------|----|
| Lakewood | Fiberline | `30000001-0000-0000-0000-000000000001` |
| Lakewood | Bleaching | `30000001-0000-0000-0000-000000000002` |
| Lakewood | Paper Machine 1 | `30000001-0000-0000-0000-000000000003` |
| Lakewood | Recovery Boiler | `30000001-0000-0000-0000-000000000004` |
| Riverside | Fiberline | `30000002-0000-0000-0000-000000000001` |
| Riverside | Chemical Recovery | `30000002-0000-0000-0000-000000000002` |
| Riverside | Paper Machine 1 | `30000002-0000-0000-0000-000000000003` |
| Riverside | Utilities | `30000002-0000-0000-0000-000000000004` |

### Devices

#### Lakewood Mill

| Code | Name | Area | Device ID |
|------|------|------|-----------|
| PUMP-LW-001 | Feed Water Pump | Fiberline | `40000001-0000-0000-0000-000000000001` |
| PUMP-LW-002 | White Liquor Pump | Fiberline | `40000001-0000-0000-0000-000000000002` |
| MOTOR-LW-001 | Chip Feeder Motor | Fiberline | `40000001-0000-0000-0000-000000000003` |
| DGST-LW-001 | Continuous Digester | Fiberline | `40000001-0000-0000-0000-000000000004` |
| PUMP-LW-003 | Bleach Pump | Bleaching | `40000001-0000-0000-0000-000000000005` |
| RFNR-LW-001 | Primary Refiner | Paper Machine 1 | `40000001-0000-0000-0000-000000000006` |
| PUMP-LW-004 | PM1 Head Box Pump | Paper Machine 1 | `40000001-0000-0000-0000-000000000007` |
| MOTOR-LW-002 | PM1 Drive Motor | Paper Machine 1 | `40000001-0000-0000-0000-000000000008` |
| PUMP-LW-005 | Recovery Boiler Feed Pump | Recovery Boiler | `40000001-0000-0000-0000-000000000009` |
| MOTOR-LW-003 | Recovery Boiler Fan Motor | Recovery Boiler | `40000001-0000-0000-0000-000000000010` |

#### Riverside Mill

| Code | Name | Area | Device ID |
|------|------|------|-----------|
| PUMP-RV-001 | Feed Water Pump | Fiberline | `40000002-0000-0000-0000-000000000001` |
| MOTOR-RV-001 | Chip Feeder Motor | Fiberline | `40000002-0000-0000-0000-000000000002` |
| DGST-RV-001 | Batch Digester | Fiberline | `40000002-0000-0000-0000-000000000003` |
| PUMP-RV-002 | Black Liquor Pump | Chemical Recovery | `40000002-0000-0000-0000-000000000004` |
| PUMP-RV-003 | Green Liquor Pump | Chemical Recovery | `40000002-0000-0000-0000-000000000005` |
| MOTOR-RV-002 | Recovery Fan Motor | Chemical Recovery | `40000002-0000-0000-0000-000000000006` |
| RFNR-RV-001 | Primary Refiner | Paper Machine 1 | `40000002-0000-0000-0000-000000000007` |
| PUMP-RV-004 | PM1 White Water Pump | Paper Machine 1 | `40000002-0000-0000-0000-000000000008` |
| PUMP-RV-005 | Cooling Water Pump | Utilities | `40000002-0000-0000-0000-000000000009` |
| MOTOR-RV-003 | Main Drive Motor | Utilities | `40000002-0000-0000-0000-000000000010` |

### Alert Thresholds

| Device | Metric | Condition | Severity | Consecutive | Threshold ID |
|--------|--------|-----------|----------|-------------|-------------|
| PUMP-LW-001 | bearing_temp | > 75°C | HIGH | 3 | `50000001-...-0001` |
| PUMP-LW-001 | bearing_temp | > 85°C | CRITICAL | 2 | `50000001-...-0002` |
| PUMP-LW-001 | vibration | > 8 mm/s | HIGH | 3 | `50000001-...-0003` |
| PUMP-LW-001 | flow_rate | < 20 m³/h | CRITICAL | 3 | `50000001-...-0004` |
| DGST-LW-001 | pressure | > 7.5 bar | HIGH | 3 | `50000001-...-0005` |
| DGST-LW-001 | pressure | > 8.0 bar | CRITICAL | 2 | `50000001-...-0006` |
| DGST-LW-001 | temperature | > 180°C | HIGH | 2 | `50000001-...-0007` |
| MOTOR-LW-001 | winding_temp | > 105°C | CRITICAL | 2 | `50000001-...-0008` |
| MOTOR-LW-001 | vibration | > 10 mm/s | HIGH | 3 | `50000001-...-0009` |
| RFNR-LW-001 | plate_gap | < 0.02 mm | CRITICAL | 3 | `50000001-...-0010` |
| RFNR-LW-001 | motor_temp | > 95°C | HIGH | 3 | `50000001-...-0011` |
| MOTOR-LW-002 | winding_temp | > 100°C | HIGH | 3 | `50000001-...-0012` |
| MOTOR-LW-002 | vibration | > 12 mm/s | CRITICAL | 2 | `50000001-...-0013` |
| PUMP-RV-001 | bearing_temp | > 80°C | HIGH | 3 | `50000002-...-0001` |
| PUMP-RV-001 | vibration | > 8 mm/s | HIGH | 3 | `50000002-...-0002` |
| DGST-RV-001 | pressure | > 8.0 bar | CRITICAL | 2 | `50000002-...-0003` |
| DGST-RV-001 | temperature | > 178°C | HIGH | 2 | `50000002-...-0004` |
| PUMP-RV-002 | pump_temp | > 90°C | HIGH | 3 | `50000002-...-0005` |
| PUMP-RV-002 | flow_rate | < 15 m³/h | HIGH | 3 | `50000002-...-0006` |
| MOTOR-RV-003 | winding_temp | > 110°C | CRITICAL | 2 | `50000002-...-0007` |
| MOTOR-RV-003 | vibration | > 9 mm/s | HIGH | 3 | `50000002-...-0008` |

---

## Prerequisites for Live Demo

Before running the alert scenarios you need:

1. **API running:** `dotnet run --project src/backend/EdgePulse.API`
2. **TelemetryProcessor running:** `dotnet run --project src/backend/EdgePulse.TelemetryProcessor`
3. **RabbitMQ running:** `docker-compose up rabbitmq` (or use the full stack)
4. **Auth token:** Get a Keycloak token for the NordPulp CustomerAdmin user

```bash
# Get token (adjust credentials to your Keycloak setup)
TOKEN=$(curl -s -X POST http://localhost:8080/realms/edgepulse/protocol/openid-connect/token \
  -d 'grant_type=password&client_id=edgepulse-api&client_secret=lnBQYXdQnQTku1jT64LbEMyaRFRws3HS' \
  -d 'username=nordpulp-admin&password=Test@1234' | jq -r .access_token)
```

---

## Demo Script: 5 Alert Scenarios

### Scenario 1 — CRITICAL: Bearing Overheat (Lakewood Feed Water Pump)

Demonstrates a critical pump bearing failure scenario. Sends 3 consecutive readings
with bearing temperature above 85°C.

```bash
# Device: PUMP-LW-001
# Threshold: bearing_temp > 85°C CRITICAL (2 consecutive)
# We send 2 readings above threshold

DEVICE_ID="40000001-0000-0000-0000-000000000001"
TENANT_ID="10000001-0000-0000-0000-000000000001"
MILL_ID="20000001-0000-0000-0000-000000000001"
AREA_ID="30000001-0000-0000-0000-000000000001"

for i in 1 2; do
  curl -s -X POST http://localhost:5170/api/telemetry \
    -H "Content-Type: application/json" \
    -d "{
      \"deviceId\": \"$DEVICE_ID\",
      \"tenantId\": \"$TENANT_ID\",
      \"millId\":   \"$MILL_ID\",
      \"areaId\":   \"$AREA_ID\",
      \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
      \"metrics\": [
        {\"key\": \"bearing_temp\", \"value\": 87.3, \"unit\": \"°C\"},
        {\"key\": \"vibration\",    \"value\": 4.2,  \"unit\": \"mm/s\"},
        {\"key\": \"flow_rate\",    \"value\": 145.0,\"unit\": \"m³/h\"}
      ]
    }"
  sleep 2
done

echo "Check alerts: curl -s http://localhost:5170/api/alerts?statusCode=OPEN -H 'Authorization: Bearer $TOKEN' | jq"
```

### Scenario 2 — HIGH: Digester Pressure Rising (Lakewood Continuous Digester)

Demonstrates a digester pressure trend approaching safe limits.

```bash
DEVICE_ID="40000001-0000-0000-0000-000000000004"

# Send 3 readings above 7.5 bar (HIGH threshold)
for i in 1 2 3; do
  curl -s -X POST http://localhost:5170/api/telemetry \
    -H "Content-Type: application/json" \
    -d "{
      \"deviceId\": \"$DEVICE_ID\",
      \"tenantId\": \"$TENANT_ID\",
      \"millId\":   \"$MILL_ID\",
      \"areaId\":   \"$AREA_ID\",
      \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
      \"metrics\": [
        {\"key\": \"pressure\",    \"value\": 7.8, \"unit\": \"bar\"},
        {\"key\": \"temperature\", \"value\": 168.0, \"unit\": \"°C\"}
      ]
    }"
  sleep 2
done
```

### Scenario 3 — CRITICAL: Chip Feeder Motor Overheating (Lakewood)

Motor winding temperature above 105°C — immediate shutdown required.

```bash
DEVICE_ID="40000001-0000-0000-0000-000000000003"
AREA_ID="30000001-0000-0000-0000-000000000001"

for i in 1 2; do
  curl -s -X POST http://localhost:5170/api/telemetry \
    -H "Content-Type: application/json" \
    -d "{
      \"deviceId\": \"$DEVICE_ID\",
      \"tenantId\": \"$TENANT_ID\",
      \"millId\":   \"$MILL_ID\",
      \"areaId\":   \"$AREA_ID\",
      \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
      \"metrics\": [
        {\"key\": \"winding_temp\", \"value\": 108.5, \"unit\": \"°C\"},
        {\"key\": \"vibration\",    \"value\": 6.1,  \"unit\": \"mm/s\"}
      ]
    }"
  sleep 2
done
```

### Scenario 4 — CRITICAL: Riverside Digester Pressure Spike

Cross-mill scenario showing alerts from a different location.

```bash
DEVICE_ID="40000002-0000-0000-0000-000000000003"
MILL_ID_RV="20000001-0000-0000-0000-000000000002"
AREA_ID_RV="30000002-0000-0000-0000-000000000001"

for i in 1 2; do
  curl -s -X POST http://localhost:5170/api/telemetry \
    -H "Content-Type: application/json" \
    -d "{
      \"deviceId\": \"$DEVICE_ID\",
      \"tenantId\": \"$TENANT_ID\",
      \"millId\":   \"$MILL_ID_RV\",
      \"areaId\":   \"$AREA_ID_RV\",
      \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
      \"metrics\": [
        {\"key\": \"pressure\",    \"value\": 8.4, \"unit\": \"bar\"},
        {\"key\": \"temperature\", \"value\": 172.0, \"unit\": \"°C\"}
      ]
    }"
  sleep 2
done
```

### Scenario 5 — Acknowledge + Resolve Workflow

After running any scenario above, demonstrate the alert lifecycle:

```bash
# 1. See open alerts
curl -s "http://localhost:5170/api/alerts?statusCode=OPEN" \
  -H "Authorization: Bearer $TOKEN" | jq '.items[0]'

# 2. Get the alert ID from the response above
ALERT_ID="<paste-alert-id-here>"

# 3. Acknowledge it
curl -s -X POST "http://localhost:5170/api/alerts/$ALERT_ID/acknowledge" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notes": "Bearing cooling initiated. Monitoring temperature trend."}'

# 4. Resolve it
curl -s -X POST "http://localhost:5170/api/alerts/$ALERT_ID/resolve" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"notes": "Bearing replaced. Temperature stable at 62°C."}'

# 5. Confirm status
curl -s "http://localhost:5170/api/alerts/$ALERT_ID" \
  -H "Authorization: Bearer $TOKEN" | jq '{status: .statusCode, resolvedBy: .resolvedBy}'
```

---

## Verify the Seed via API

```bash
# List devices
curl -s "http://localhost:5170/api/devices" \
  -H "Authorization: Bearer $TOKEN" | jq '[.[] | {code, name, millName, areaName}]'

# List alert thresholds for Lakewood Feed Water Pump
curl -s "http://localhost:5170/api/alerts/thresholds?deviceId=40000001-0000-0000-0000-000000000001" \
  -H "Authorization: Bearer $TOKEN" | jq '[.[] | {name, metricKey, maxValue, severityCode}]'

# Open alert count (sidebar badge)
curl -s "http://localhost:5170/api/alerts/count" \
  -H "Authorization: Bearer $TOKEN" | jq
```

---

## Re-seeding / Reset

To completely reset the demo data:

```bash
dotnet run --project src/backend/EdgePulse.API -- --seed
```

The seed will:
1. Detect the existing tenant by slug `nordpulp`
2. Delete it (cascade-deletes all mills, areas, devices, thresholds, alerts)
3. Re-create everything with fixed demo IDs

> **Warning:** This also deletes any alerts created during demos. Export them first if needed.

---

## Demo Data Values Rationale

All threshold values come from Pulp & Paper industry standards:

| Metric | Normal Range | Warning | Critical | Source |
|--------|-------------|---------|----------|--------|
| Pump bearing temp | 40–70°C | >75°C | >85°C | ISO 10816-3 |
| Motor winding temp | <80°C | >90°C | >105°C | IEC 60034-1 |
| Digester pressure | 5–7 bar | >7.5 bar | >8.0 bar | TAPPI TIP 0402-23 |
| Digester temperature | 160–175°C | >176°C | >180°C | Kraft process spec |
| Pump vibration | <4 mm/s | >6 mm/s | >8 mm/s | ISO 10816-1 |
| Motor vibration | <4.5 mm/s | >7 mm/s | >10 mm/s | ISO 10816-3 |
| Refiner plate gap | 0.1–0.3 mm | <0.05 mm | <0.02 mm | Mill operations |
| Flow rate (feed pump) | >100 m³/h | <50 m³/h | <20 m³/h | Process engineering |
