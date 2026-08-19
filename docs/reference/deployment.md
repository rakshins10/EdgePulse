# Deployment Guides

## 1. Local development

Follow the [Setup guide](../guides/01-setup-guide.md) — Docker infra +
`dotnet run` + `npm run dev`. This is the everyday loop.

## 2. On-premise (single host, Docker)

The demo compose file is the blueprint
(`infrastructure/docker-compose.onpremise.yml`). For a real site:

1. **Use the released images** from GHCR instead of local builds:
   ```yaml
   image: ghcr.io/rakshins10/edgepulse-api:1.0.0
   image: ghcr.io/rakshins10/edgepulse-telemetry-processor:1.0.0
   image: ghcr.io/rakshins10/edgepulse-dashboard:1.0.0
   image: ghcr.io/rakshins10/edgepulse-ingestion:1.0.0
   image: ghcr.io/rakshins10/edgepulse-opcua-agent:1.0.0
   ```
2. **Persist volumes** for SQL Server, MongoDB, RabbitMQ, Postgres and the
   API's attachment root (`/app/data/attachments`).
3. **Change every default credential** (SQL sa, Mongo, RabbitMQ, Keycloak
   admin) and set them via environment, not baked files.
4. Point `Smtp` at the site's mail relay; keep MailHog out of production.
5. Put the dashboard + API behind **HAProxy** (config included) with TLS
   termination; Keycloak must be served over HTTPS for non-localhost.
6. Run Keycloak in production mode (`start` with a database, not
   `start-dev`).
7. Apply migrations on upgrade:
   `dotnet ef database update` (or run the API with migrations gate)
   before starting the new API/processor version.
8. **AI alert explanations (optional).** The on-prem stack includes an
   `ollama` service (`ollama/ollama:0.5.7`, container `edgepulse-ollama`,
   port 11434, model volume `edgepulse_ollama_models`, `mem_limit: 4g`) and a
   one-shot `ollama-pull` service that downloads `llama3.2` (~2 GB) once.
   Point the API at it with `Ai__Provider=ollama` and
   `Ai__Ollama__BaseUrl=http://ollama:11434` (in-network name). No internet
   is needed after the pull and alert text never leaves the host. Sites that
   do not want the feature set `Ai__Provider=none` and may drop both
   services — the API and dashboard degrade cleanly (no ✦ Explain button).

Sizing (demo-scale, 20 devices @ 5 s): everything fits in 4 vCPU / 8 GB.
MongoDB disk grows ~1–2 GB/month at that rate — plan retention.
With Ollama enabled add ~3 GB RAM for the model (first call ~40 s, then
5–15 s on CPU; on demand + cached, so no steady-state load).

## 3. Cloud (Azure) sketch

The architecture doc's cloud profile maps 1-to-1:

| On-prem | Azure |
|---------|-------|
| SQL Server container | Azure SQL |
| MongoDB | Cosmos DB (Mongo API), partition key `deviceId` |
| RabbitMQ | Azure Service Bus |
| Keycloak | Keycloak on Container Apps or Entra External ID |
| Containers | Azure Container Apps (images from GHCR) |
| Attachments volume | Azure Blob (implement `IFileStorage` for blob) |
| MailHog | ACS Email / SendGrid |
| Ollama (`Ai:Provider=ollama`) | Azure OpenAI (`Ai:Provider=azureopenai`, endpoint + deployment + key via env) |

`DEPLOYMENT_MODE` selects DI registrations where the implementations differ.

## 4. CI/CD

GitHub Actions (see [`docs/devops/01-cicd-guide.md`](../devops/01-cicd-guide.md)):

- **CI** on every push/PR — backend build + 130 unit tests, dashboard build.
- **Publish** per component on merge (beta `X.Y.Z-beta.N`, from the
  changelog's Unreleased target) and on `component-vX.Y.Z` tags (release
  `:X.Y.Z`, `:X.Y`, `:latest` + GitHub Release).
- Images: `ghcr.io/rakshins10/edgepulse-{api,telemetry-processor,dashboard,ingestion,opcua-agent}`.

Release procedure: [`docs/devops/02-releasing.md`](../devops/02-releasing.md).
