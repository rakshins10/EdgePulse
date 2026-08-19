# EdgePulse -- Docker Commands Reference

All commands run from the root of the project:
  C:\Studies\EdgePulse-Application
  or in Git Bash: /c/Studies/EdgePulse-Application

---

## On-Premise Stack Commands

### Start all services (detached / background)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

### Start all services (with logs visible)
docker compose -f infrastructure/docker-compose.onpremise.yml up

### Stop all services (keeps data volumes)
docker compose -f infrastructure/docker-compose.onpremise.yml down

### Stop all services AND delete all data volumes
### WARNING: this deletes all database data permanently
docker compose -f infrastructure/docker-compose.onpremise.yml down -v

### Restart a single service
docker compose -f infrastructure/docker-compose.onpremise.yml restart keycloak
docker compose -f infrastructure/docker-compose.onpremise.yml restart sqlserver
docker compose -f infrastructure/docker-compose.onpremise.yml restart mongodb
docker compose -f infrastructure/docker-compose.onpremise.yml restart rabbitmq

### Check status of all services
docker compose -f infrastructure/docker-compose.onpremise.yml ps

### View logs of all services
docker compose -f infrastructure/docker-compose.onpremise.yml logs

### View logs of a specific service (live / follow)
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f keycloak
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f sqlserver
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f mongodb
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f rabbitmq
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f haproxy
docker compose -f infrastructure/docker-compose.onpremise.yml logs -f ollama

### Pull latest images (update all services)
docker compose -f infrastructure/docker-compose.onpremise.yml pull

### Rebuild and restart (after config changes)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d --force-recreate

---

## Individual Container Commands

### Enter a running container (bash shell)
docker exec -it edgepulse-sqlserver bash
docker exec -it edgepulse-mongodb bash
docker exec -it edgepulse-rabbitmq bash
docker exec -it edgepulse-keycloak bash

### Check container resource usage (CPU, RAM)
docker stats

### Check logs of specific container
docker logs edgepulse-sqlserver
docker logs edgepulse-mongodb --tail 50
docker logs edgepulse-keycloak -f

---

## SQL Server Commands (inside container)

### Connect to SQL Server via sqlcmd inside container
docker exec -it edgepulse-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P EdgePulse@2026 -No

### Run a quick query to verify SQL Server is working
docker exec -it edgepulse-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P EdgePulse@2026 \
  -Q "SELECT @@VERSION" -No

---

## MongoDB Commands (inside container)

### Connect to MongoDB shell inside container
docker exec -it edgepulse-mongodb \
  mongosh -u edgepulse -p EdgePulse@2026 \
  --authenticationDatabase admin

### Quick check MongoDB is working
docker exec -it edgepulse-mongodb \
  mongosh -u edgepulse -p EdgePulse@2026 \
  --authenticationDatabase admin \
  --eval "db.adminCommand('ping')"

---

## RabbitMQ Commands (inside container)

### List all queues
docker exec -it edgepulse-rabbitmq \
  rabbitmqctl list_queues

### List all connections
docker exec -it edgepulse-rabbitmq \
  rabbitmqctl list_connections

---

## Ollama / AI Commands (alert explanations)

### Start Ollama and pull the llama3.2 model (~2 GB, once; kept in the model volume)
docker compose -f infrastructure/docker-compose.onpremise.yml up -d ollama ollama-pull

### Watch the model download
docker logs -f edgepulse-ollama-pull

### Check which models are available (ready when llama3.2 is listed)
curl http://localhost:11434/api/tags

### Ollama logs
docker logs edgepulse-ollama --tail 50
docker logs edgepulse-ollama -f

### Test the model directly (no EdgePulse involved)
curl http://localhost:11434/api/chat -d '{"model":"llama3.2","stream":false,"messages":[{"role":"user","content":"Say hello in five words"}]}'

### Re-run the pull if the API logs "model 'llama3.2' not found"
docker compose -f infrastructure/docker-compose.onpremise.yml up -d ollama-pull

### Stop Ollama (model stays in the volume)
docker compose -f infrastructure/docker-compose.onpremise.yml stop ollama

### Remove the downloaded model (frees ~2 GB; next start re-pulls)
docker compose -f infrastructure/docker-compose.onpremise.yml rm -sf ollama ollama-pull
docker volume rm edgepulse_ollama_models

---

## Docker System Commands

### List all running containers
docker ps

### List all containers including stopped
docker ps -a

### List all downloaded images
docker images

### Remove unused images (free up disk space)
docker image prune

### Remove stopped containers, unused networks, dangling images
docker system prune

### Check Docker disk usage
docker system df

---

## Services in the stack

| Service | Container | Host port | Notes |
|---------|-----------|-----------|-------|
| SQL Server 2022 | `edgepulse-sqlserver` | 1433 | primary DB |
| MongoDB 7 | `edgepulse-mongodb` | 27017 | telemetry time-series |
| RabbitMQ 3.12 | `edgepulse-rabbitmq` | 5672 / 15672 (UI) | telemetry queue |
| PostgreSQL 16 | `edgepulse-postgres` | 5432 | Keycloak's DB |
| Keycloak 24 | `edgepulse-keycloak` | 8080 | identity |
| MailHog | `edgepulse-mailhog` | 1025 (SMTP) / 8025 (UI) | catches alert emails locally |
| HAProxy | `edgepulse-haproxy` | 80 / 8404 (stats) | load balancer |
| OPC-UA simulator | `edgepulse-opcua-simulator` | 4840 | NordPulp plant (20 devices) |
| OPC-UA agent | `edgepulse-opcua-agent` | — | publishes telemetry to RabbitMQ |
| Ingestion (NestJS) | `edgepulse-ingestion` | 3000* | REST telemetry ingest |
| Ollama | `edgepulse-ollama` | 11434 | local LLM for alert explanations (llama3.2) |
| Ollama pull | `edgepulse-ollama-pull` | — | one-shot: downloads the model, then exits |

*Only when started; the dashboard dev server also uses 3000 on the host.

## Service URLs (On-Premise Mode)

Service          URL                          Login
-----------      ---------------------------  -------------------------
HAProxy Stats    http://localhost:8404/stats  admin / edgepulse123
Keycloak Admin   http://localhost:8080        admin / admin
RabbitMQ UI      http://localhost:15672       edgepulse / EdgePulse@2026
SQL Server       localhost:1433               sa / EdgePulse@2026
MongoDB          localhost:27017              edgepulse / EdgePulse@2026
Device API       http://localhost:5000        (JWT from Keycloak)
Telemetry Svc    http://localhost:3000        (Device API Key)
Ollama           http://localhost:11434       (no auth, local only)
React Dashboard  http://localhost:4000        (JWT from Keycloak)

---

## Volume Names (persistent data)

edgepulse_postgres_data    -> Keycloak database
edgepulse_sqlserver_data   -> Devices, alerts, users
edgepulse_mongodb_data     -> Telemetry readings
edgepulse_rabbitmq_data    -> RabbitMQ messages
edgepulse_ollama_models    -> Ollama model files (llama3.2, ~2 GB)

---

## Common Issues & Fixes

### Stale port binding after a long stop (Docker Desktop)
`compose start` fails with `Bind for 0.0.0.0:27017 failed: port is already allocated`
but nothing on the host is listening. Docker's network layer has a stale
binding — recreate the container (named volumes keep the data):
```powershell
docker compose -f infrastructure/docker-compose.onpremise.yml up -d --force-recreate mongodb
```
A container that returns HTTP 000 / connection-refused despite "Up" is the
same class of problem: `docker restart <container>`.

### Running sqlcmd / mongosh from Git Bash
Git Bash rewrites `/opt/...` paths to Windows paths inside `docker exec`.
Prefix with `MSYS_NO_PATHCONV=1`:
```bash
MSYS_NO_PATHCONV=1 docker exec edgepulse-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'EdgePulse@2026' -C -d EdgePulse -Q "SELECT COUNT(*) FROM Devices"
```

### Port already in use
netstat -ano | findstr :1433
taskkill /PID <pid> /F

### Container keeps restarting
docker logs edgepulse-sqlserver --tail 20

### SQL Server health check failing
# Give it 60 seconds on first start -- it is slow to initialize

### Keycloak not starting
# Check postgres is healthy first:
docker compose -f infrastructure/docker-compose.onpremise.yml ps postgres

### AI "Explain" says it did not return a summary
# First call loads the model (~40 s); retry. Then check Ollama is up and has the model:
docker logs edgepulse-ollama --tail 20
curl http://localhost:11434/api/tags

### Reset everything and start fresh (DELETES ALL DATA)
docker compose -f infrastructure/docker-compose.onpremise.yml down -v
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

---

## Stop Before Restart / Shutdown

### Always stop containers before shutting down Windows
docker compose -f infrastructure/docker-compose.onpremise.yml down

### Verify all stopped
docker ps
# Should show empty list