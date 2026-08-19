# Integration Guides

How to get data in and events out.

## 1. OPC-UA (primary on-premise path)

The **edge agent** (`src/EdgePulse.OpcUaAgent`) subscribes to OPC-UA
variables and publishes readings to RabbitMQ.

1. **Discover** the server:
   ```bash
   cd src/EdgePulse.OpcUaAgent
   npm run discover -- opc.tcp://plc-host:4840
   ```
   Prints the device/variable inventory and a ready-to-paste `devices[]`
   config snippet (one metric per variable, snake_cased keys).
2. **Register** each device in EdgePulse (Devices → Register) and copy its
   ids into the snippet (`deviceId`, `tenantId`, `millId`, `areaId`).
3. Drop the snippet into the agent config and start the agent
   (Docker service `opcua-agent` or `npm run start`).

Security: the agent connects with `SecurityMode.None` by default (typical
inside an OT network segment); certificates can be enabled via node-opcua
options if the server requires them.

## 2. Device simulator

`npm run simulate` (or the `opcua-simulator` container) exposes a full
NordPulp plant — 20 devices / 52 variables with realistic drift and
occasional threshold breaches. Ideal for demos and E2E testing.

## 3. REST ingestion (protocol-agnostic)

For anything that can POST JSON:

```
POST http://<ingestion>:3000/ingest
X-Device-Key: <device API key from registration>

{ "metrics": [ { "key": "bearing_temp", "value": 61.2, "unit": "°C" } ] }
```

The ingestion service validates the key against the API, stamps
tenant/mill/area and publishes to RabbitMQ. Keys are SHA-256-hashed at
rest and revoked on decommission.

## 4. Direct queue publish (advanced)

Trusted producers inside the network may publish
`TelemetryReading` JSON directly to RabbitMQ queue `telemetry.readings`
(vhost `edgepulse`) — the processor deserializes case-insensitively.

## 5. Outbound webhooks (events out)

Admin UI: 🔗 Integrations. Events: `alert.created`,
`workorder.created`.

- **JSON format** (default):
  ```json
  { "event": "alert.created", "timestamp": "…", "data": { … } }
  ```
  Headers: `X-EdgePulse-Event`, and `X-EdgePulse-Signature` =
  lowercase hex `HMAC-SHA256(secret, rawBody)`. Verify server-side:
  ```js
  crypto.createHmac('sha256', SECRET).update(rawBody).digest('hex') === sig
  ```
- **Slack format**: `{"text": ":zap: EdgePulse `alert.created` — …"}` —
  paste a Slack or Teams incoming-webhook URL and it works unmodified.
- Delivery is best-effort with a 10 s timeout; the last status/time is
  shown per subscription, and **Send test** fires a signed sample.

## 6. Alert e-mail

Every alert also emails the configured recipients
(`Smtp` section, TelemetryProcessor). Local dev captures everything in
MailHog (http://localhost:8025).

## 7. AI providers (alert explanations)

The ✦ Explain button on the Alerts page asks a language model for a short
"what happened / likely causes / recommended action" text. The API talks to
the model through one abstraction, `IAiAssistant`, and the provider is
chosen by `Ai:Provider`:

| Provider | `Ai:Provider` | Where the model runs | Data leaves the network? |
|----------|---------------|----------------------|--------------------------|
| Ollama (default on-prem) | `ollama` | `edgepulse-ollama` container, `http://ollama:11434`, model `llama3.2` | No |
| Azure OpenAI (cloud profile) | `azureopenai` | Azure endpoint + deployment (key via env/user-secrets) | Yes — alert facts (device, metric, values) are sent to Azure |
| Disabled | `none` | — | — |

Endpoints: `GET /api/ai/status` and `GET /api/ai/alerts/{id}/summary`
(see the API reference). Summaries are generated on demand and cached on the
alert; the provider can be swapped by configuration without code changes.
Details: [AI guide](../guides/05-ai-guide.md).

## 8. Roadmap (not yet implemented)

Modbus TCP and MQTT broker ingestion are planned post-v1.0; until then the
REST path covers those sources via a small adapter at the edge.
