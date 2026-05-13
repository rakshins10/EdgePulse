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
React Dashboard  http://localhost:4000        (JWT from Keycloak)

---

## Volume Names (persistent data)

edgepulse_postgres_data    -> Keycloak database
edgepulse_sqlserver_data   -> Devices, alerts, users
edgepulse_mongodb_data     -> Telemetry readings
edgepulse_rabbitmq_data    -> RabbitMQ messages

---

## Common Issues & Fixes

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