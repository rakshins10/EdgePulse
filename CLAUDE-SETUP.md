# EdgePulse -- Development Setup & Commands Guide

Complete step-by-step record of all setup commands and project structure.
Use this as reference when setting up on a new machine or onboarding new developers.

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Repository Setup](#2-repository-setup)
3. [Local Infrastructure Setup](#3-local-infrastructure-setup)
4. [.NET Solution Structure](#4-net-solution-structure)
5. [Node.js Telemetry Service Setup](#5-nodejs-telemetry-service-setup)
6. [React Dashboard Setup](#6-react-dashboard-setup)
7. [Database Setup](#7-database-setup)
8. [Keycloak Configuration](#8-keycloak-configuration)
9. [Running the Full Stack](#9-running-the-full-stack)
10. [Git Workflow](#10-git-workflow)

---

## 1. Prerequisites

### Required Tools

```
Tool                Version     Download
────────────────    ─────────   ──────────────────────────────────────
.NET SDK            9.x         https://dotnet.microsoft.com/download
Node.js             20.x        https://nodejs.org
Docker Desktop      Latest      https://docker.com/products/docker-desktop
Git                 Latest      https://git-scm.com
VS Code             Latest      https://code.visualstudio.com
SSMS                Latest      https://aka.ms/ssmsfullsetup
MongoDB Compass     Latest      https://www.mongodb.com/try/download/compass
```

### VS Code Extensions

```
Claude Code                 -> AI pair programmer (reads CLAUDE.md)
C# Dev Kit                  -> .NET development
ESLint                      -> JavaScript/TypeScript linting
Prettier                    -> Code formatting
Docker                      -> Docker file support
GitLens                     -> Git history and blame
REST Client                 -> Test API endpoints (.http files)
```

### Verify Installation

```bash
dotnet --version        # should show 9.x.x
node --version          # should show 20.x.x
docker --version        # should show 24.x or later
docker compose version  # should show v2.x or later
git --version           # any recent version
```

---

## 2. Repository Setup

### Clone The Repository

```bash
git clone https://github.com/rakshins10/EdgePulse.git
cd EdgePulse
```

### Repository Structure

```
EdgePulse/
  src/
    EdgePulse.Domain/           <- .NET 9 class library
    EdgePulse.Application/      <- .NET 9 class library
    EdgePulse.Infrastructure/   <- .NET 9 class library
    EdgePulse.API/              <- .NET 9 Web API
    EdgePulse.TelemetryService/ <- Node.js / NestJS (TODO)
    EdgePulse.Processor/        <- .NET 9 Worker Service (TODO)
    EdgePulse.Dashboard/        <- React + TypeScript (TODO)
  tools/
    DeviceSimulator/            <- .NET 9 console app (TODO)
  infrastructure/
    docker-compose.onpremise.yml
    docker-compose.cloud.yml    <- TODO
    haproxy/
      haproxy.cfg
    mongo/
      init.js
    sql/                        <- EF Core migration scripts
  docs/
    01-requirements.md
    02-architecture.md
    03-data-design.md
    04-api-design.md            <- TODO
    05-identity-design.md       <- TODO
    06-infrastructure.md        <- TODO
  CLAUDE.md
  CLAUDE-SETUP.md               <- this file
  DOCKER-COMMANDS.md
  README.md
  LICENSE
```

---

## 3. Local Infrastructure Setup

### Start On-Premise Stack

All local development uses the on-premise Docker Compose stack.
This runs all infrastructure services in Docker containers.

```bash
# From project root
cd /c/Studies/EdgePulse-Application

# Start all services in background
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

# Check all services are running
docker compose -f infrastructure/docker-compose.onpremise.yml ps
```

### Expected Output After Start

```
NAME                    STATUS              PORTS
edgepulse-haproxy       running             0.0.0.0:80->80, 0.0.0.0:8404->8404
edgepulse-keycloak      running (healthy)   0.0.0.0:8080->8080
edgepulse-mongodb       running (healthy)   0.0.0.0:27017->27017
edgepulse-postgres      running (healthy)   0.0.0.0:5432->5432
edgepulse-rabbitmq      running (healthy)   0.0.0.0:5672->5672, 0.0.0.0:15672->15672
edgepulse-sqlserver     running (healthy)   0.0.0.0:1433->1433
```

### Service URLs

```
Service          URL                          Login
───────────────  ───────────────────────────  ──────────────────────────
HAProxy Stats    http://localhost:8404/stats  admin / edgepulse123
Keycloak Admin   http://localhost:8080        admin / admin
RabbitMQ UI      http://localhost:15672       edgepulse / EdgePulse@2026
SQL Server       localhost:1433               sa / EdgePulse@2026
MongoDB          localhost:27017              edgepulse / EdgePulse@2026
```

### Stop Stack

```bash
# Stop containers (data is preserved in volumes)
docker compose -f infrastructure/docker-compose.onpremise.yml down

# Always stop before shutting down Windows
```

---

## 4. .NET Solution Structure

### Step 1 -- Create Solution And Projects

```bash
cd /c/Studies/EdgePulse-Application

# Create src folder
mkdir -p src

# Create solution file
dotnet new sln -n EdgePulse -o src

# Create Clean Architecture projects
dotnet new classlib -n EdgePulse.Domain        -o src/EdgePulse.Domain
dotnet new classlib -n EdgePulse.Application   -o src/EdgePulse.Application
dotnet new classlib -n EdgePulse.Infrastructure -o src/EdgePulse.Infrastructure
dotnet new webapi   -n EdgePulse.API           -o src/EdgePulse.API
```

### Step 2 -- Add Projects To Solution

```bash
cd src

dotnet sln EdgePulse.sln add EdgePulse.Domain/EdgePulse.Domain.csproj
dotnet sln EdgePulse.sln add EdgePulse.Application/EdgePulse.Application.csproj
dotnet sln EdgePulse.sln add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj
dotnet sln EdgePulse.sln add EdgePulse.API/EdgePulse.API.csproj
```

### Step 3 -- Add Project References

```bash
# Application depends on Domain
dotnet add EdgePulse.Application/EdgePulse.Application.csproj \
  reference EdgePulse.Domain/EdgePulse.Domain.csproj

# Infrastructure depends on Application and Domain
dotnet add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj \
  reference EdgePulse.Application/EdgePulse.Application.csproj

dotnet add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj \
  reference EdgePulse.Domain/EdgePulse.Domain.csproj

# API depends on all three
dotnet add EdgePulse.API/EdgePulse.API.csproj \
  reference EdgePulse.Application/EdgePulse.Application.csproj

dotnet add EdgePulse.API/EdgePulse.API.csproj \
  reference EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj

dotnet add EdgePulse.API/EdgePulse.API.csproj \
  reference EdgePulse.Domain/EdgePulse.Domain.csproj
```

### Step 4 -- Verify Build

```bash
cd /c/Studies/EdgePulse-Application/src
dotnet build
# Expected: Build succeeded. 0 Error(s)
```

### Step 5 -- Install NuGet Packages

```bash
# IMPORTANT: Always pin package versions to match your .NET version
# .NET 9 -> use package version 9.x.x
# Do NOT use latest -- NuGet may install .NET 10 incompatible packages

cd /c/Studies/EdgePulse-Application/EdgePulse/src

# MediatR -- CQRS pattern
dotnet add EdgePulse.Application/EdgePulse.Application.csproj \
  package MediatR

# FluentValidation -- input validation
dotnet add EdgePulse.Application/EdgePulse.Application.csproj \
  package FluentValidation

dotnet add EdgePulse.Application/EdgePulse.Application.csproj \
  package FluentValidation.DependencyInjectionExtensions

# EF Core -- database access (pinned to 9.0.5 for .NET 9)
dotnet add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj \
  package Microsoft.EntityFrameworkCore --version 9.0.5

dotnet add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj \
  package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.5

dotnet add EdgePulse.Infrastructure/EdgePulse.Infrastructure.csproj \
  package Microsoft.EntityFrameworkCore.Tools --version 9.0.5

# EF Core design tools (needed for migrations) -- pinned to 9.0.5
dotnet add EdgePulse.API/EdgePulse.API.csproj \
  package Microsoft.EntityFrameworkCore.Design --version 9.0.5

# JWT Authentication -- pinned to 9.0.5
dotnet add EdgePulse.API/EdgePulse.API.csproj \
  package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.5

# Swagger
dotnet add EdgePulse.API/EdgePulse.API.csproj \
  package Swashbuckle.AspNetCore
```

> Note: EF Core 10 requires .NET 10. Since we use .NET 9, always specify
> --version 9.0.5 for all Microsoft.EntityFrameworkCore.* packages.
> Same rule applies to Microsoft.AspNetCore.* packages.

### Step 6 -- Clean Up Default Files

```bash
# Remove default files created by templates
rm src/EdgePulse.Domain/Class1.cs
rm src/EdgePulse.Application/Class1.cs
rm src/EdgePulse.Infrastructure/Class1.cs

# Remove default WeatherForecast files from API
rm src/EdgePulse.API/Controllers/WeatherForecastController.cs
rm src/EdgePulse.API/WeatherForecast.cs
```

### Clean Architecture Folder Structure

After setup, create this folder structure inside each project:

```
src/
  EdgePulse.Domain/
    Entities/               <- Device, Mill, Area, Alert, Tenant
    Enums/                  <- DeviceStatus, AlertSeverity, UserRole etc.
    Interfaces/             <- IRepository, IUnitOfWork
    ValueObjects/           <- strongly typed IDs, coordinates
    Exceptions/             <- domain-specific exceptions

  EdgePulse.Application/
    Common/
      Behaviours/           <- MediatR pipeline behaviours
      Interfaces/           <- IApplicationDbContext
      Exceptions/           <- application exceptions
    Features/
      Devices/
        Commands/           <- RegisterDevice, UpdateDevice
        Queries/            <- GetDevices, GetDeviceById
        DTOs/               <- DeviceDto, DeviceListDto
      Mills/
      Areas/
      Alerts/
      Users/

  EdgePulse.Infrastructure/
    Persistence/
      Configurations/       <- EF Core entity configurations
      Migrations/           <- EF Core migrations
      EdgePulseDbContext.cs
    Services/               <- external service implementations
    Repositories/           <- repository implementations

  EdgePulse.API/
    Controllers/            <- HTTP endpoints
    Middleware/             <- JWT validation, error handling
    Extensions/             <- service registration helpers
    appsettings.json
    appsettings.Development.json
    Program.cs
```

### Run The API

```bash
cd /c/Studies/EdgePulse-Application/src/EdgePulse.API
dotnet run

# API available at:
# http://localhost:5000
# https://localhost:5001
# Swagger: http://localhost:5000/swagger
```

### EF Core Migration Commands

All migration commands run from the `src` folder.
Always specify `--project` and `--startup-project`.

```
--project         = where DbContext lives (Infrastructure)
--startup-project = where Program.cs lives (API)
                    needed to read appsettings.json for connection string
```

#### Install EF Core CLI Tool (one time only)

```bash
# Install globally
dotnet tool install --global dotnet-ef

# Update if already installed
dotnet tool update --global dotnet-ef

# Verify installation
dotnet ef --version
```

#### Create A Migration

```bash
# Creates a new migration file in Infrastructure/Persistence/Migrations/
# Migration name should describe what changed
# EF Core compares current entity state with last migration and generates SQL

dotnet ef migrations add InitialCreate \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

dotnet ef migrations add AddAlertThresholdsTable \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Apply Migrations To Database

```bash
# Applies all pending migrations to the database
# Creates database if it does not exist
# Also runs HasData() seed data

dotnet ef database update \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Apply up to a specific migration (partial update)
dotnet ef database update InitialCreate \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Remove Last Migration

```bash
# Removes the most recently created migration FILE
# Only works if that migration has NOT been applied to database yet
# If already applied -- rollback first, then remove

dotnet ef migrations remove \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Rollback A Migration

```bash
# Rolls back database to a previous migration
# Does NOT delete migration files -- only undoes the SQL

# Roll back to a specific migration
dotnet ef database update PreviousMigrationName \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Roll back ALL migrations (empty database)
dotnet ef database update 0 \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### List All Migrations

```bash
# Shows all migrations and whether applied or pending
dotnet ef migrations list \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Generate SQL Script

```bash
# Generates SQL file instead of applying directly
# Useful for reviewing, DBA approval, CI/CD pipelines

dotnet ef migrations script \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API \
  --output infrastructure/sql/migration.sql

# Idempotent script (safe to run multiple times)
dotnet ef migrations script --idempotent \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API \
  --output infrastructure/sql/migration-idempotent.sql
```

#### Drop The Database

```bash
# Completely drops the database -- all data lost
# Development use only

dotnet ef database drop \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Full Reset (development only)

```bash
# Step 1: Drop database
dotnet ef database drop \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Step 2: Delete all migration files manually
rm src/EdgePulse.Infrastructure/Persistence/Migrations/*.cs

# Step 3: Create fresh migration
dotnet ef migrations add InitialCreate \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API

# Step 4: Apply
dotnet ef database update \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Adding A New Entity -- Standard Workflow

```bash
# 1. Create entity in EdgePulse.Domain/Entities/
# 2. Add IQueryable<T> to IApplicationDbContext interface
# 3. Add DbSet<T> to EdgePulseDbContext
# 4. Create configuration in Infrastructure/Persistence/Configurations/
# 5. Create migration
dotnet ef migrations add Add<EntityName>Table \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
# 6. Review generated migration file -- make sure SQL looks correct
# 7. Apply migration
dotnet ef database update \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

#### Common Errors & Fixes

```bash
# Error: "Build failed"
# Fix: Run dotnet build first and fix all errors
dotnet build

# Error: "CASCADE DELETE cycle"
# Fix: Add OnDelete(DeleteBehavior.NoAction) in entity configuration
#      on the FK causing the cycle, remove migration and recreate

# Error: "Unable to connect to database"
# Fix: Check Docker containers are running
docker compose -f infrastructure/docker-compose.onpremise.yml ps

# Error: "Migration already applied, cannot remove"
# Fix: Rollback first, then remove
dotnet ef database update PreviousMigrationName \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
dotnet ef migrations remove \
  --project EdgePulse.Infrastructure \
  --startup-project EdgePulse.API
```

---

## 5. Node.js Telemetry Service Setup

### Create NestJS Project

```bash
cd /c/Studies/EdgePulse-Application/src

# Install NestJS CLI globally
npm install -g @nestjs/cli

# Create NestJS project
nest new EdgePulse.TelemetryService --package-manager npm

cd EdgePulse.TelemetryService

# Install required packages
npm install @nestjs/config
npm install class-validator class-transformer
npm install @azure/service-bus        # cloud mode
npm install amqplib @nestjs/microservices  # on-premise mode
```

### Run Telemetry Service

```bash
cd /c/Studies/EdgePulse-Application/src/EdgePulse.TelemetryService

# Development mode (with hot reload)
npm run start:dev

# Service available at: http://localhost:3000
```

---

## 6. React Dashboard Setup

### Create React Project

```bash
cd /c/Studies/EdgePulse-Application/src

# Create React + TypeScript project using Vite
npm create vite@latest EdgePulse.Dashboard -- --template react-ts

cd EdgePulse.Dashboard
npm install

# Install required packages
npm install react-router-dom
npm install @tanstack/react-query
npm install axios
npm install recharts
npm install keycloak-js
npm install tailwindcss @tailwindcss/vite
```

### Run Dashboard

```bash
cd /c/Studies/EdgePulse-Application/src/EdgePulse.Dashboard

npm run dev

# Dashboard available at: http://localhost:5173
```

---

## 7. Database Setup

### SQL Server (via SSMS)

```
Connection details:
  Server name:    localhost,1433
  Authentication: SQL Server Authentication
  Login:          sa
  Password:       EdgePulse@2026
  Options:        Trust server certificate = YES

Create database manually (first time):
  Right click Databases -> New Database -> EdgePulse
  Or run: CREATE DATABASE EdgePulse;

Then run EF Core migrations to create schema.
```

### MongoDB (via Compass)

```
Connection string:
  mongodb://edgepulse:EdgePulse%402026@localhost:27017/?authSource=admin

Database:    edgepulse_telemetry
Collection:  readings (auto-created by init.js)
```

---

## 8. Keycloak Configuration

### Access Admin Console

```
URL:      http://localhost:8080
Username: admin
Password: admin
```

### Create EdgePulse Realm (first time)

```
1. Click dropdown top-left (shows "master")
2. Click "Create realm"
3. Realm name: EdgePulse
4. Click Create

5. Create roles:
   Realm settings -> Roles -> Create role
   Add: SuperAdmin, CustomerAdmin, MillManager, Operator, Executive

6. Create client for API:
   Clients -> Create client
   Client ID: edgepulse-api
   Client type: OpenID Connect
   Save

7. Create client for Dashboard:
   Clients -> Create client
   Client ID: edgepulse-dashboard
   Client type: OpenID Connect
   Root URL: http://localhost:5173
   Save

8. Export realm config:
   Realm settings -> Action -> Partial export
   Save as: infrastructure/keycloak/realm-export.json
   Commit to repo
```

---

## 9. Running The Full Stack

### Start Everything (On-Premise Mode)

```bash
# Terminal 1: Infrastructure
docker compose -f infrastructure/docker-compose.onpremise.yml up -d

# Terminal 2: Device API
cd src/EdgePulse.API && dotnet run

# Terminal 3: Telemetry Service
cd src/EdgePulse.TelemetryService && npm run start:dev

# Terminal 4: Dashboard
cd src/EdgePulse.Dashboard && npm run dev
```

### All Service URLs

```
Service             URL                          Notes
──────────────────  ───────────────────────────  ──────────────────
React Dashboard     http://localhost:5173         Main UI
Device API          http://localhost:5000         .NET 9 API
Device API Swagger  http://localhost:5000/swagger API documentation
Telemetry Service   http://localhost:3000         Node.js ingestion
Keycloak            http://localhost:8080         Identity provider
RabbitMQ UI         http://localhost:15672        Message queue UI
HAProxy Stats       http://localhost:8404/stats   Load balancer stats
SQL Server          localhost:1433                Primary database
MongoDB             localhost:27017               Telemetry database
```

---

## 10. Git Workflow

### Commit Message Format

```
feat:     new feature
fix:      bug fix
docs:     documentation only
infra:    infrastructure / docker / config
test:     tests added or updated
chore:    maintenance, cleanup, dependency updates
refactor: code restructure without feature change

Examples:
  feat: add device registration endpoint
  feat: add JWT authentication middleware
  fix: resolve tenant isolation in device query
  docs: add API design document v1.0
  infra: add cloud docker compose stack
  test: add device registration unit tests
  refactor: extract device validation to pipeline behaviour
```

### Branch Strategy

```
main          <- production ready code only
              <- protected, requires PR + CI pass

feature/*     <- new features
  e.g. feature/device-registration-api
       feature/telemetry-ingestion-service
       feature/keycloak-integration

fix/*         <- bug fixes
  e.g. fix/jwt-token-expiry

docs/*        <- documentation updates
  e.g. docs/api-design-document
```

### Daily Workflow

```bash
# Start your day
git checkout main
git pull origin main
git checkout -b feature/your-feature-name

# Work on feature...

# Commit often
git add .
git commit -m "feat: description of what you did"

# Push and create PR
git push origin feature/your-feature-name
# Create PR on GitHub -> merge to main
```

### Quick Commands

```bash
# Check status
git status

# See recent commits
git log --oneline -10

# Discard local changes
git checkout .

# Stash changes temporarily
git stash
git stash pop

# See what changed
git diff
```

---

## Useful One-Liners

```bash
# Check what's running on a port (Windows)
netstat -ano | findstr :5000
netstat -ano | findstr :1433

# Kill process on port
taskkill /PID <pid> /F

# Check Docker disk usage
docker system df

# Clean up unused Docker resources
docker system prune

# Check .NET SDK versions installed
dotnet --list-sdks

# Check installed global npm packages
npm list -g --depth=0

# Restore NuGet packages
dotnet restore

# Clean build artifacts
dotnet clean
```

---

*Last updated: May 2026*
*Next step: Build EdgePulse.Domain entities and enums*
