# EdgePulse -- Solution Architecture Guide

**Version:** 1.0
**Last Updated:** May 2026
**Author:** Rakshith N S

This document explains the design principles, patterns, and
request/response flow for every project in the EdgePulse solution.

---

## Table of Contents

1. [Solution Overview](#1-solution-overview)
2. [EdgePulse.Domain](#2-edgepulsedomain)
3. [EdgePulse.Application](#3-edgepulseapplication)
4. [EdgePulse.Infrastructure](#4-edgepulseinfrastructure)
5. [EdgePulse.API](#5-edgepulseapi)
6. [Request & Response Flow](#6-request--response-flow)
7. [Dependency Rules](#7-dependency-rules)
8. [Error Handling Flow](#8-error-handling-flow)
9. [Adding A New Feature -- Step By Step](#9-adding-a-new-feature----step-by-step)

---

## 1. Solution Overview

EdgePulse follows **Clean Architecture** (also known as Onion Architecture).
The solution is divided into 4 projects, each with a specific responsibility.
Dependencies only point inward -- outer layers depend on inner layers, never
the reverse.

```
+--------------------------------------------------+
|                  EdgePulse.API                   |
|         (HTTP endpoints, middleware, DI)         |
|                       |                          |
|         depends on    |                          |
|                       v                          |
|           EdgePulse.Infrastructure               |
|    (EF Core, Azure SDKs, external services)      |
|                       |                          |
|         depends on    |                          |
|                       v                          |
|           EdgePulse.Application                  |
|   (business logic, commands, queries, handlers)  |
|                       |                          |
|         depends on    |                          |
|                       v                          |
|             EdgePulse.Domain                     |
|      (entities, enums, constants, interfaces)    |
|     depends on NOTHING -- pure C# only           |
+--------------------------------------------------+
```

### Why Clean Architecture?

```
BENEFIT                    HOW IT HELPS EDGEPULSE
─────────────────────────  ──────────────────────────────────────
Testable business logic    Application layer has no EF Core,
                           no HTTP -- pure logic, easy to unit test

Swappable infrastructure   Azure Service Bus <-> RabbitMQ
                           Cosmos DB <-> MongoDB
                           No code changes in business logic

Independent of frameworks  Domain entities are plain C# classes
                           No [Required] attributes, no EF base class

Readable by any developer  Each layer has one job
                           New developer knows exactly where to look
```

---

## 2. EdgePulse.Domain

### Purpose

The innermost layer. Contains ONLY business concepts.
Has zero dependencies on any framework or library.
This is the heart of EdgePulse.

### Design Principles

```
Principle                  Implementation
─────────────────────────  ───────────────────────────────────────
Rich Domain Model          Entities have methods, not just properties
                           e.g. device.Decommission(), tenant.Suspend()

Factory Methods            Entities are created via static Create()
                           methods -- not public constructors
                           This enforces valid state at creation

Encapsulation              All properties have private setters
                           State can only change via entity methods

No Framework Dependency    No EF Core attributes, no ASP.NET types
                           Pure C# classes only
```

### Folder Structure

```
EdgePulse.Domain/
  Common/
    BaseEntity.cs           <- Id, CreatedAt, UpdatedAt, IsDeleted
    TenantBaseEntity.cs     <- BaseEntity + TenantId
    LookupBaseEntity.cs     <- TenantBaseEntity + lookup fields

  Entities/
    -- Core Entities --
    Tenant.cs               <- Customer organisation
    Mill.cs                 <- Physical facility (belongs to Tenant)
    Area.cs                 <- Department/line (belongs to Mill)
    Device.cs               <- Physical equipment (belongs to Area)
    Attachment.cs           <- File attached to any entity
    TenantTemplate.cs       <- Links tenant to industry template

    -- Lookup Entities --
    IndustryTemplate.cs     <- Pulp & Paper, Manufacturing, Generic
    DeviceType.cs           <- Pump, Motor, Valve (configurable)
    DeviceStatus.cs         <- Online, Offline, Maintenance
    AlertSeverity.cs        <- Critical, High, Medium, Low
    AlertStatus.cs          <- Open, Acknowledged, Resolved
    MetricType.cs           <- Temperature, Pressure, Vibration
    Unit.cs                 <- C, bar, mm/s, L/min
    DeviceManufacturer.cs   <- ABB, Siemens, Bosch
    DeviceModel.cs          <- Per manufacturer models
    MaintenanceType.cs      <- Scheduled, Corrective, Preventive
    LocationType.cs         <- Building, Floor, Line, Cell
    TenantLookupOverride.cs <- Tenant customisation of template values

  Enums/
    UserRole.cs             <- SuperAdmin, CustomerAdmin, MillManager,
                               Operator, Executive
    DeploymentMode.cs       <- Cloud, OnPremise
    RoleScope.cs            <- Platform, Tenant, Mill, Area

  Constants/
    WellKnownIds.cs         <- Fixed GUIDs for seeded system values
                               Referenced in code by name, never as strings
    LookupTypes.cs          <- "DeviceType", "DeviceStatus" etc.
    EntityTypes.cs          <- "Device", "Mill", "Area" etc.
    FileCategories.cs       <- "Photo", "Manual", "Report" etc.
```

### Entity Design Pattern

Every entity follows this exact pattern:

```csharp
public class Mill : TenantBaseEntity
{
    // 1. Properties -- private setters only
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    // 2. Navigation properties
    public ICollection<Area> Areas { get; private set; }
        = new List<Area>();

    // 3. Protected constructor -- EF Core needs this
    protected Mill() { }

    // 4. Static factory method -- only way to create entity
    public static Mill Create(Guid tenantId, string name, string code, ...)
    {
        return new Mill
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    // 5. Business methods -- state changes through methods only
    public void UpdateDetails(string name, string location, ...)
    {
        Name = name;
        // ... other updates
        MarkAsUpdated(); // from BaseEntity
    }
}
```

### Well-Known GUIDs Pattern

System seed values use fixed, predictable GUIDs:

```csharp
// No magic strings. No magic numbers.
// Compile-time safe. IntelliSense works.
// Matches exactly what's seeded in database.

public static class PulpAndPaperDeviceTypeIds
{
    public static readonly Guid Pump =
        Guid.Parse("00000011-0000-0000-0000-000000000001");
    public static readonly Guid Motor =
        Guid.Parse("00000011-0000-0000-0000-000000000002");
}

// Usage -- type safe, no strings:
device.TypeId == PulpAndPaperDeviceTypeIds.Pump  // correct
device.TypeId == "PUMP"                          // compile error
```

---

## 3. EdgePulse.Application

### Purpose

Contains ALL business logic. Orchestrates the use cases of the application.
Has no knowledge of HTTP, databases, or external services.
Depends only on Domain.

### Design Principles

```
Principle                  Implementation
─────────────────────────  ───────────────────────────────────────
CQRS                       Commands (write) and Queries (read)
                           are completely separated

MediatR                    IRequest<T> and IRequestHandler<TRequest, TResponse>
                           All requests go through the mediator pipeline

Pipeline Behaviours        Cross-cutting concerns applied to ALL requests:
                           1. LoggingBehaviour  -- logs every request
                           2. ValidationBehaviour -- validates before handling

Dependency Inversion       Application defines interfaces (IApplicationDbContext,
                           ICurrentUserService, IFileStorageService)
                           Infrastructure implements them

No Framework Dependency    No EF Core attributes
                           IQueryable<T> used (not DbSet<T>)
```

### Folder Structure

```
EdgePulse.Application/
  Common/
    Behaviours/
      LoggingBehaviour.cs       <- logs every request + response
      ValidationBehaviour.cs    <- runs FluentValidation before handler

    Exceptions/
      ValidationException.cs    <- invalid input (400)
      NotFoundException.cs      <- entity not found (404)
      ForbiddenAccessException  <- no permission (403)
      ConflictException.cs      <- duplicate/conflict (409)

    Interfaces/
      IApplicationDbContext.cs  <- database abstraction (IQueryable)
      ICurrentUserService.cs    <- who is making this request
      IFileStorageService.cs    <- upload/download files

  Features/
    Devices/
      Commands/
        CreateDeviceCommand.cs  <- IRequest<Guid>
        UpdateDeviceCommand.cs  <- IRequest
        DecommissionDeviceCommand.cs
      Queries/
        GetDevicesQuery.cs      <- IRequest<List<DeviceDto>>
        GetDeviceByIdQuery.cs   <- IRequest<DeviceDto>
      DTOs/
        DeviceDto.cs            <- what API returns
        DeviceListDto.cs        <- list view

    Mills/       (same pattern)
    Areas/       (same pattern)
    Alerts/      (same pattern)
    Configuration/

  DependencyInjection.cs        <- registers MediatR, validators, behaviours
```

### CQRS Pattern

```
COMMANDS (write operations -- change state):
  RegisterDeviceCommand
  UpdateDeviceCommand
  DecommissionDeviceCommand
  AcknowledgeAlertCommand

  Rules:
  -> Always go through validation behaviour first
  -> Return: Guid (new entity id) or Unit (no return value)
  -> Recorded in audit log
  -> Throw exceptions on failure (never return null)

QUERIES (read operations -- no state change):
  GetDevicesQuery
  GetDeviceByIdQuery
  GetDeviceTypesQuery
  GetMillReportQuery

  Rules:
  -> No validation behaviour (reads are safe)
  -> Return: DTO or List<DTO> or null (if not found)
  -> NEVER modify state
  -> Use AsNoTracking for performance (no EF change tracking)
```

### Command Example

```csharp
// 1. Command -- the request
public record RegisterDeviceCommand(
    Guid AreaId,
    Guid TypeId,
    string Name,
    string Code
) : IRequest<Guid>;

// 2. Validator -- FluentValidation
public class RegisterDeviceCommandValidator
    : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AreaId).NotEmpty();
        RuleFor(x => x.TypeId).NotEmpty();
    }
}

// 3. Handler -- the business logic
public class RegisterDeviceCommandHandler
    : IRequestHandler<RegisterDeviceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<Guid> Handle(
        RegisterDeviceCommand request,
        CancellationToken cancellationToken)
    {
        // Business logic here
        var device = Device.Create(...);
        _context.Add(device);
        await _context.SaveChangesAsync(cancellationToken);
        return device.Id;
    }
}
```

### Query Example

```csharp
// 1. Query -- the request
public record GetDeviceTypesQuery : IRequest<List<DeviceTypeDto>>;

// 2. DTO -- what we return (never the entity itself)
public record DeviceTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsSystem,
    int SortOrder
);

// 3. Handler -- read only, no state change
public class GetDeviceTypesQueryHandler
    : IRequestHandler<GetDeviceTypesQuery, List<DeviceTypeDto>>
{
    public async Task<List<DeviceTypeDto>> Handle(
        GetDeviceTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.DeviceTypes
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            .OrderBy(x => x.SortOrder)
            .Select(x => new DeviceTypeDto(x.Id, x.Name, ...))
            .ToListAsync(cancellationToken);
    }
}
```

### MediatR Pipeline Order

Every request passes through this pipeline in order:

```
Request received
      |
      v
1. LoggingBehaviour
   -> Logs: RequestName, UserId, TenantId
      |
      v
2. ValidationBehaviour (Commands only)
   -> Runs all FluentValidation validators
   -> Throws ValidationException if invalid
      |
      v
3. Handler
   -> Executes business logic
      |
      v
Response returned
```

---

## 4. EdgePulse.Infrastructure

### Purpose

Implements all interfaces defined in Application.
Contains EF Core, Azure SDKs, external service clients.
This is where framework-specific code lives.

### Design Principles

```
Principle                  Implementation
─────────────────────────  ───────────────────────────────────────
Implement, Don't Define    Implements IApplicationDbContext,
                           ICurrentUserService, IFileStorageService

EF Core Global Filters     Every tenant-scoped entity has automatic
                           WHERE TenantId = current_tenant filter
                           Developer cannot forget this filter

Entity Configurations      Each entity has its own configuration class
                           implementing IEntityTypeConfiguration<T>
                           Keeps DbContext clean

Seed Data in Config        HasData() in entity configurations
                           System data seeded via EF Core migrations

Strategy Pattern           IFileStorageService ->
                             AzureBlobStorageService (cloud)
                             MinioStorageService (on-premise)
                           DEPLOYMENT_MODE env var switches at runtime
```

### Folder Structure

```
EdgePulse.Infrastructure/
  Persistence/
    Configurations/
      IndustryTemplateConfiguration.cs  <- table mapping + seed data
      DeviceTypeConfiguration.cs        <- table mapping + seed data
      DeviceConfiguration.cs            <- FK cascade rules
      -- one file per entity --

    Migrations/
      -- auto-generated by EF Core --
      -- never edit manually --

    EdgePulseDbContext.cs               <- implements IApplicationDbContext

  Services/
    CurrentUserService.cs               <- reads JWT claims from HttpContext
    AzureBlobStorageService.cs          <- cloud file storage (TODO)
    MinioStorageService.cs              <- on-premise file storage (TODO)

  DependencyInjection.cs                <- registers all services
```

### EF Core DbContext Design

```csharp
public class EdgePulseDbContext : DbContext, IApplicationDbContext
{
    // Implements IApplicationDbContext interface
    // Application layer uses interface -- never this class directly

    // Global Query Filters applied in OnModelCreating:
    // Every tenant-scoped entity auto-filters by TenantId + IsDeleted
    // Developer cannot write a query that leaks cross-tenant data

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Apply ALL configurations from this assembly automatically
        builder.ApplyConfigurationsFromAssembly(
            typeof(EdgePulseDbContext).Assembly);
    }
}
```

### Entity Configuration Pattern

```csharp
// One configuration class per entity
public class DeviceTypeConfiguration
    : IEntityTypeConfiguration<DeviceType>
{
    public void Configure(EntityTypeBuilder<DeviceType> builder)
    {
        // 1. Table name
        builder.ToTable("DeviceTypes");

        // 2. Primary key
        builder.HasKey(x => x.Id);

        // 3. Column constraints
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        // 4. Indexes
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        // 5. Relationships (if any)
        // ...

        // 6. Seed data (system defaults)
        builder.HasData(
            new { Id = PulpAndPaperDeviceTypeIds.Pump,
                  Name = "Pump", Code = "PUMP", ... }
        );
    }
}
```

### Cascade Delete Rules

SQL Server rejects multiple cascade paths on the same table.
Device has FKs to: Mill, Area, DeviceType, DeviceStatus, Manufacturer, Model.

```
Rule applied in DeviceConfiguration:
  Area  -> Device: CASCADE  (primary relationship)
  Mill  -> Device: NO ACTION
  Type  -> Device: NO ACTION
  Status-> Device: NO ACTION
  Manufacturer -> Device: NO ACTION
  Model -> Device: NO ACTION

Reason: When an Area is deleted (soft delete), cascade to devices.
        All other FK deletes must be handled manually in business logic.
```

---

## 5. EdgePulse.API

### Purpose

HTTP entry point. Routes requests to MediatR. Returns HTTP responses.
Contains no business logic -- only wiring.

### Design Principles

```
Principle                  Implementation
─────────────────────────  ───────────────────────────────────────
Thin Controllers           Controllers only call IMediator.Send()
                           No business logic in controllers

Problem Details (RFC 7807) All errors return consistent problem JSON
                           via global exception middleware

Versioning                 API versioned from day one (/api/v1/)
                           Breaking changes get new version

Swagger/OpenAPI            All endpoints documented
                           Request/response schemas auto-generated

JWT Authentication         Keycloak issues JWT
                           API validates on every request (TODO)
```

### Folder Structure

```
EdgePulse.API/
  Controllers/
    ConfigurationController.cs  <- lookup table endpoints
    DevicesController.cs        <- device CRUD (TODO)
    MillsController.cs          <- mill management (TODO)
    AreasController.cs          <- area management (TODO)
    AlertsController.cs         <- alert lifecycle (TODO)
    UsersController.cs          <- user management (TODO)

  Middleware/
    ExceptionHandlingMiddleware.cs  <- global error handler (TODO)

  Extensions/
    ServiceCollectionExtensions.cs  <- DI setup helpers (TODO)

  Program.cs              <- app startup, pipeline configuration
  appsettings.json        <- configuration (no secrets)
  appsettings.Development.json  <- local overrides
```

### Controller Pattern

```csharp
[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigurationController(IMediator mediator)
    {
        _mediator = mediator;
        // Only dependency is IMediator
        // Controller knows nothing about business logic
    }

    [HttpGet("device-types")]
    [ProducesResponseType(typeof(List<DeviceTypeDto>), 200)]
    public async Task<IActionResult> GetDeviceTypes(
        CancellationToken cancellationToken)
    {
        // 1. Create query/command
        // 2. Send to mediator
        // 3. Return result
        // No business logic here
        var result = await _mediator.Send(
            new GetDeviceTypesQuery(), cancellationToken);
        return Ok(result);
    }
}
```

### HTTP Status Code Standards

```
200 OK           -> Successful GET
201 Created      -> Successful POST (new resource)
204 No Content   -> Successful PUT/DELETE
400 Bad Request  -> Validation failed (ValidationException)
401 Unauthorized -> Not authenticated (no/invalid JWT)
403 Forbidden    -> Authenticated but no permission (ForbiddenAccessException)
404 Not Found    -> Entity not found (NotFoundException)
409 Conflict     -> Duplicate/conflict (ConflictException)
500 Server Error -> Unexpected error
```

---

## 6. Request & Response Flow

### Complete Flow -- GET /api/configuration/device-types

```
[1] HTTP Request
    GET /api/configuration/device-types
    Authorization: Bearer <jwt_token>
           |
           v
[2] ASP.NET Core Middleware Pipeline
    -> JWT validation (TODO: Keycloak)
    -> Routing
    -> Controller selection
           |
           v
[3] ConfigurationController.GetDeviceTypes()
    -> Creates: new GetDeviceTypesQuery()
    -> Calls: await _mediator.Send(query)
           |
           v
[4] MediatR Pipeline
    -> LoggingBehaviour.Handle()
       Logs: "EdgePulse Request: GetDeviceTypesQuery UserId: ... TenantId: ..."
           |
           v
[5] GetDeviceTypesQueryHandler.Handle()
    -> _context.DeviceTypes        (IQueryable<DeviceType>)
    -> .Where(!IsDeleted && IsActive)
    -> .Where(TenantId == null || TenantId == currentUser.TenantId)
    -> .OrderBy(SortOrder)
    -> .Select(x => new DeviceTypeDto(...))
    -> .ToListAsync()
           |
           v
[6] EF Core translates to SQL:
    SELECT Id, Name, Code, Description, Icon, IsSystem, SortOrder
    FROM DeviceTypes
    WHERE IsDeleted = 0
    AND IsActive = 1
    AND (TenantId IS NULL OR TenantId = '00000099-...')
    ORDER BY SortOrder ASC
           |
           v
[7] SQL Server executes query
    Returns 5 rows (Pulp & Paper device types)
           |
           v
[8] EF Core maps rows to List<DeviceTypeDto>
           |
           v
[9] Handler returns List<DeviceTypeDto> to MediatR
           |
           v
[10] LoggingBehaviour logs: "EdgePulse Response: GetDeviceTypesQuery completed"
           |
           v
[11] Controller receives List<DeviceTypeDto>
     return Ok(result)  -> HTTP 200
           |
           v
[12] ASP.NET Core serializes to JSON
     Returns response to client
```

### Complete Flow -- POST /api/devices (Command)

```
[1] HTTP Request
    POST /api/devices
    Authorization: Bearer <jwt_token>
    Body: { "areaId": "...", "typeId": "...", "name": "PUMP-LW-001" }
           |
           v
[2] ASP.NET Core
    -> JWT validation -> extracts userId, tenantId, role
    -> Model binding -> deserializes JSON to RegisterDeviceCommand
           |
           v
[3] DevicesController.RegisterDevice()
    -> Calls: await _mediator.Send(command)
           |
           v
[4] MediatR Pipeline
    -> LoggingBehaviour.Handle()
           |
           v
[5] ValidationBehaviour.Handle()
    -> Runs RegisterDeviceCommandValidator
    -> Checks: Name not empty, Code not empty, AreaId valid
    -> If INVALID: throws ValidationException
       -> Returns HTTP 400 with error details
    -> If VALID: continues
           |
           v
[6] RegisterDeviceCommandHandler.Handle()
    -> Verify area belongs to current tenant
    -> Verify device code unique within tenant
    -> var device = Device.Create(...)
    -> _context.Add(device)
    -> await _context.SaveChangesAsync()
    -> return device.Id
           |
           v
[7] EF Core generates and executes SQL:
    INSERT INTO Devices (Id, TenantId, AreaId, TypeId, ...)
    VALUES (...)
           |
           v
[8] Returns new device Guid to controller
           |
           v
[9] Controller returns HTTP 201 Created
    Location: /api/devices/{newId}
```

---

## 7. Dependency Rules

```
ALLOWED dependencies (pointing inward only):
  API           -> Infrastructure  YES
  API           -> Application     YES
  API           -> Domain          YES
  Infrastructure-> Application     YES
  Infrastructure-> Domain          YES
  Application   -> Domain          YES

FORBIDDEN dependencies (pointing outward):
  Domain        -> Application     NO
  Domain        -> Infrastructure  NO
  Domain        -> API             NO
  Application   -> Infrastructure  NO
  Application   -> API             NO
  Infrastructure-> API             NO

HOW IT IS ENFORCED:
  .csproj references only allow inward dependencies
  Domain.csproj has zero project references
  Application.csproj references Domain only
  Infrastructure.csproj references Application + Domain
  API.csproj references all three
```

---

## 8. Error Handling Flow

```
Exception thrown anywhere in pipeline
          |
          v
Global Exception Middleware (TODO: ExceptionHandlingMiddleware)
          |
          v
Maps exception to HTTP response:

ValidationException     -> 400 Bad Request
  Body: { "errors": { "Name": ["Name is required"] } }

NotFoundException       -> 404 Not Found
  Body: { "title": "Device (uuid) was not found." }

ForbiddenAccessException-> 403 Forbidden
  Body: { "title": "You do not have permission." }

ConflictException       -> 409 Conflict
  Body: { "title": "Device code PUMP-LW-001 already exists." }

Any other Exception     -> 500 Internal Server Error
  Body: { "title": "An error occurred." }
  Detail: only shown in Development environment
```

---

## 9. Adding A New Feature -- Step By Step

Example: Add `RegisterDevice` feature

```
STEP 1: Domain (if new entity needed)
  -> Create entity in EdgePulse.Domain/Entities/
  -> Add to IApplicationDbContext
  -> Add DbSet to EdgePulseDbContext
  -> Create EF configuration
  -> Create migration

STEP 2: Application -- Command
  -> Create RegisterDeviceCommand.cs in
     EdgePulse.Application/Features/Devices/Commands/
     Contains: Command record, Validator, Handler

STEP 3: Application -- Query (if needed)
  -> Create GetDevicesQuery.cs in
     EdgePulse.Application/Features/Devices/Queries/
     Contains: Query record, DTO, Handler

STEP 4: API -- Controller endpoint
  -> Add method to DevicesController.cs
  -> POST /api/devices -> calls RegisterDeviceCommand
  -> GET /api/devices  -> calls GetDevicesQuery

STEP 5: Test in Swagger
  -> dotnet run
  -> http://localhost:5000/swagger
  -> Execute endpoint
  -> Verify response

STEP 6: Commit
  -> git add + git commit with descriptive message
```

### File Naming Conventions

```
Commands:     RegisterDeviceCommand.cs
              UpdateDeviceCommand.cs
              DecommissionDeviceCommand.cs

Queries:      GetDevicesQuery.cs
              GetDeviceByIdQuery.cs

DTOs:         DeviceDto.cs
              DeviceListDto.cs
              DeviceDetailDto.cs

Controllers:  DevicesController.cs (plural)

Configs:      DeviceConfiguration.cs (singular)

Migrations:   auto-named by EF Core
```

---

*Document ends.*
*Maintained alongside code -- update when patterns change.*
