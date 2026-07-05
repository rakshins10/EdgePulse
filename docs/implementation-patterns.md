# EdgePulse — Implementation Patterns Guide

> This document extracts the recurring patterns from how we implement stories.
> A new developer should be able to read this and know exactly how we work.
> Last updated: May 2026

---

## Table of Contents

1. [Story Lifecycle](#1-story-lifecycle)
2. [Feature Implementation Order](#2-feature-implementation-order)
3. [Query Pattern](#3-query-pattern)
4. [Command Pattern](#4-command-pattern)
5. [Controller Pattern](#5-controller-pattern)
6. [Lookup Write Pattern](#6-lookup-write-pattern)
7. [Delete Safety Pattern](#7-delete-safety-pattern)
8. [Role Scoping Pattern](#8-role-scoping-pattern)
9. [EF Core Configuration Pattern](#9-ef-core-configuration-pattern)
10. [Testing Checklist](#10-testing-checklist)
11. [PR & Commit Checklist](#11-pr--commit-checklist)
12. [Common Mistakes & Fixes](#12-common-mistakes--fixes)

---

## 1. Story Lifecycle

Every story follows this exact lifecycle without exception:

```
BEFORE CODING:
  1. Pick story from Backlog on GitHub Project board
  2. Move card to "In Progress"
  3. Assign yourself to the issue
  4. Create branch: git checkout -b feature/US-XXX-short-description

WHILE CODING:
  5. Implement in order: Domain → Application → Infrastructure → API
  6. Commit with issue reference: git commit -m "feat: description #XX"
  7. Stop API between builds (files get locked otherwise)

AFTER CODING:
  8. Run API: cd src/backend/EdgePulse.API && dotnet run
  9. Test every acceptance criterion in Swagger
  10. Move card to "In Review"
  11. git push origin feature/US-XXX-...
  12. gh pr create --title "..." --body "... Closes #XX ..."

AFTER TESTING PASSES:
  13. Merge PR on GitHub
  14. Issue auto-closes (because PR body has "Closes #XX")
  15. Move card to "Done" (or automatic if workflow enabled)
  16. git checkout main && git pull && git branch -d feature/US-XXX-...
```

---

## 2. Feature Implementation Order

Always implement in this order. Never skip layers.

```
Step 1: Domain (if new entity or method needed)
  └── Add entity to EdgePulse.Domain/Entities/
  └── Add factory method: Entity.Create(...) or Entity.CreateCustomValue(...)
  └── Add domain methods: entity.Deactivate(), entity.Rename(), etc.

Step 2: Application — Command or Query
  └── Create Command/Query record (IRequest<T>)
  └── Create Validator (AbstractValidator<TCommand>)
  └── Create Handler (IRequestHandler<TCommand, TResult>)
  └── All in one file: XxxCommand.cs or XxxQuery.cs

Step 3: Infrastructure (if new entity)
  └── Add IEntityTypeConfiguration<T> in Configurations/
  └── Add DbSet + IQueryable to EdgePulseDbContext
  └── Add IQueryable<T> to IApplicationDbContext interface
  └── Run: dotnet ef migrations add <Name>
  └── Run: dotnet ef database update

Step 4: API
  └── Add endpoint to controller
  └── Add XxxRequest record (separate from Command)
  └── Map request → command in action method

Step 5: Test
  └── All acceptance criteria in Swagger
  └── Document results in PR description
```

---

## 3. Query Pattern

Every read operation follows this template:

```csharp
// File: Features/Devices/Queries/GetDeviceTypesQuery.cs

using EdgePulse.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Queries;

// 1. Query record — optional filter parameters
public record GetDeviceTypesQuery : IRequest<List<DeviceTypeDto>>;

// 2. DTO — flat projection, no navigation properties
public record DeviceTypeDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Icon,
    bool IsSystem,
    int SortOrder
);

// 3. Handler — read only, never mutates state
public class GetDeviceTypesQueryHandler
    : IRequestHandler<GetDeviceTypesQuery, List<DeviceTypeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetDeviceTypesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<DeviceTypeDto>> Handle(
        GetDeviceTypesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.DeviceTypes
            // 4. Tenant scoping: system (null) + tenant custom
            .Where(x => !x.IsDeleted && x.IsActive)
            .Where(x => x.TenantId == null ||
                        x.TenantId == _currentUser.TenantId)
            // 5. Consistent ordering
            .OrderBy(x => x.IsSystem ? 0 : 1)
            .ThenBy(x => x.SortOrder)
            // 6. Project to DTO (never return entities)
            .Select(x => new DeviceTypeDto(
                x.Id, x.Name, x.Code,
                x.Description, x.Icon,
                x.IsSystem, x.SortOrder))
            .ToListAsync(cancellationToken);
    }
}
```

### Query Rules

- Always project to a DTO — never return raw entities
- Always use `cancellationToken` in `ToListAsync`
- System values: `TenantId == null`; Custom values: `TenantId == _currentUser.TenantId`
- Order system values before custom values where applicable
- Never mutate any state in a query handler

---

## 4. Command Pattern

Every write operation follows this template:

```csharp
// File: Features/Devices/Commands/CreateDeviceTypeCommand.cs

using EdgePulse.Application.Common.Exceptions;
using EdgePulse.Application.Common.Interfaces;
using EdgePulse.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EdgePulse.Application.Features.Devices.Commands;

// 1. Command record — input parameters
public record CreateDeviceTypeCommand(
    string Name,
    string Code,
    string? Description,
    string? Icon,
    int SortOrder = 0
) : IRequest<Guid>;

// 2. Validator — in same file
public class CreateDeviceTypeCommandValidator
    : AbstractValidator<CreateDeviceTypeCommand>
{
    public CreateDeviceTypeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50)
            .Matches("^[A-Z0-9_]+$").WithMessage(
                "Code must be uppercase letters, numbers and underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(300).When(x => x.Description != null);
    }
}

// 3. Handler
public class CreateDeviceTypeCommandHandler
    : IRequestHandler<CreateDeviceTypeCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateDeviceTypeCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(
        CreateDeviceTypeCommand request,
        CancellationToken cancellationToken)
    {
        // 4. Role check first
        if (_currentUser.IsOperator || _currentUser.IsExecutive)
            throw new ForbiddenAccessException();

        // 5. Business rule: duplicate code check
        var exists = await _context.DeviceTypes
            .AnyAsync(x =>
                x.TenantId == _currentUser.TenantId &&
                x.Code == request.Code.ToUpperInvariant() &&
                !x.IsDeleted,
                cancellationToken);

        if (exists)
            throw new ConflictException(
                $"Device type with code '{request.Code}' already exists.");

        // 6. Create entity via factory method
        var deviceType = DeviceType.CreateCustomValue(
            tenantId: _currentUser.TenantId,
            name: request.Name,
            code: request.Code,
            description: request.Description,
            icon: request.Icon,
            sortOrder: request.SortOrder);

        // 7. Add and save
        _context.Add(deviceType);
        await _context.SaveChangesAsync(cancellationToken);

        // 8. Return ID only (not full entity)
        return deviceType.Id;
    }
}
```

### Command Rules

- Validator and Handler in the **same file** as the Command record
- Role check is always **first** in the handler
- Duplicate checks use `AnyAsync` — never `FirstOrDefaultAsync` just to check existence
- Always use `ToUpperInvariant()` when storing/comparing codes
- Use entity factory methods (`Device.Create(...)`) not constructor calls
- Return only the new `Id` from create commands — not full DTOs

---

## 5. Controller Pattern

```csharp
// Controllers/ConfigurationController.cs

[ApiController]
[Route("api/[controller]")]
public class ConfigurationController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConfigurationController(IMediator mediator)
        => _mediator = mediator;

    // =============================================
    // SECTION COMMENT — helps navigation in large files
    // =============================================

    /// <summary>
    /// Swagger documentation here. Explain the business rule.
    /// System types cannot be deleted — use TenantLookupOverride.
    /// </summary>
    [HttpGet("device-types")]
    [ProducesResponseType(typeof(List<DeviceTypeDto>), 200)]
    public async Task<IActionResult> GetDeviceTypes(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetDeviceTypesQuery(), cancellationToken);
        return Ok(result);
    }

    // POST uses a Request model — NOT the Command directly
    [HttpPost("device-types")]
    [ProducesResponseType(typeof(Guid), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateDeviceType(
        [FromBody] CreateDeviceTypeRequest request,  // ← Request, not Command
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new CreateDeviceTypeCommand(                // ← Map to Command here
                request.Name, request.Code,
                request.Description, request.Icon,
                request.SortOrder),
            cancellationToken);
        return CreatedAtAction(nameof(GetDeviceTypes), new { }, id);
    }
}

// Request models at bottom of controller file
public record CreateDeviceTypeRequest(
    string Name,
    string Code,
    string? Description,
    string? Icon,
    int SortOrder = 0
);
```

### Controller Rules

- **Never** use `[FromBody] XxxCommand` — always use `[FromBody] XxxRequest`
- Request models go at the **bottom** of the controller file
- Always include `[ProducesResponseType]` for all possible status codes
- Use `CreatedAtAction` for 201 responses
- Use `NoContent()` for 204 responses (PUT, DELETE)
- Always pass `CancellationToken` to mediator

---

## 6. Lookup Write Pattern

All lookup table write operations (DeviceType, DeviceStatus, AlertSeverity, etc.) follow
an identical pattern. When implementing a new lookup:

### Create Command Checklist

```
✓ Role check: Operators and Executives cannot create
✓ Duplicate code check: AnyAsync within tenant
✓ Code normalization: request.Code.ToUpperInvariant()
✓ Factory method: XxxEntity.CreateCustomValue(tenantId, ...)
✓ Return: new entity Id
```

### Update Command Checklist

```
✓ Role check: Operators and Executives cannot update
✓ Existence check: FirstOrDefaultAsync where !IsDeleted
✓ System check: if (entity.IsSystem) throw ForbiddenAccessException
✓ Ownership check: if (entity.TenantId != _currentUser.TenantId) throw Forbidden
✓ Note: Code is NOT updatable (would break references)
✓ Update via domain method: entity.UpdateDetails(name, description)
```

### Delete Command Checklist

```
✓ Role check
✓ Existence check (check ALL records, not just tenant-owned)
✓ System check (403 if system)
✓ Ownership check (403 if wrong tenant)
✓ In-use check (409 if referenced by active records)
✓ Soft delete: entity.Deactivate()
```

**Critical:** Check existence across ALL records first (not just tenant-owned),
then check IsSystem. If you filter by TenantId first, system values (TenantId=null)
will return NotFoundException instead of ForbiddenException.

```csharp
// WRONG — system values return 404 instead of 403
var entity = await _context.DeviceStatuses
    .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _currentUser.TenantId && !x.IsDeleted)
    ?? throw new NotFoundException(...);

// CORRECT — existence first, then ownership/system checks
var entity = await _context.DeviceStatuses
    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
    ?? throw new NotFoundException(...);
if (entity.IsSystem) throw new ForbiddenAccessException();
if (entity.TenantId != _currentUser.TenantId) throw new ForbiddenAccessException();
```

---

## 7. Delete Safety Pattern

The full safe delete sequence for any entity:

```csharp
public async Task Handle(DeleteXxxCommand request, CancellationToken ct)
{
    // 1. Role check
    if (_currentUser.IsOperator || _currentUser.IsExecutive)
        throw new ForbiddenAccessException();

    // 2. Existence (across all, not tenant-filtered)
    var entity = await _context.XxxItems
        .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct)
        ?? throw new NotFoundException(nameof(XxxItem), request.Id);

    // 3. System protection
    if (entity.IsSystem)
        throw new ForbiddenAccessException();

    // 4. Ownership
    if (entity.TenantId != _currentUser.TenantId)
        throw new ForbiddenAccessException();

    // 5. In-use check (entity-specific)
    var inUse = await _context.RelatedItems
        .AnyAsync(x => x.XxxItemId == request.Id && !x.IsDeleted, ct);
    if (inUse)
        throw new ConflictException("Cannot delete — referenced by active records.");

    // 6. Soft delete
    entity.Deactivate();
    _context.Update(entity);
    await _context.SaveChangesAsync(ct);
}
```

---

## 8. Role Scoping Pattern

Role-scoped queries limit results based on the authenticated user's role and assigned scope.

### Pattern

```csharp
// Start with full tenant scope
var query = _context.Devices
    .Where(x => !x.IsDeleted && x.TenantId == _currentUser.TenantId);

// Narrow for MillManager
if (_currentUser.IsMillManager && _currentUser.MillId.HasValue)
    query = query.Where(x => x.MillId == _currentUser.MillId.Value);

// Narrow further for Operator
if (_currentUser.IsOperator && _currentUser.AreaIds.Any())
    query = query.Where(x => _currentUser.AreaIds.Contains(x.AreaId));

// SuperAdmin: no additional filtering (sees everything)
// CustomerAdmin: no additional filtering (sees full tenant)
```

### Hierarchy

```
SuperAdmin    → no filter (all tenants, or as specified)
CustomerAdmin → TenantId filter only
MillManager   → TenantId + MillId filter
Operator      → TenantId + AreaIds filter
Executive     → TenantId filter (read-only access)
```

---

## 9. EF Core Configuration Pattern

Every entity needs an `IEntityTypeConfiguration<T>` in Infrastructure:

```csharp
// Infrastructure/Persistence/Configurations/DeviceTypeConfiguration.cs

public class DeviceTypeConfiguration : IEntityTypeConfiguration<DeviceType>
{
    public void Configure(EntityTypeBuilder<DeviceType> builder)
    {
        // 1. Table name (explicit)
        builder.ToTable("DeviceTypes");
        builder.HasKey(x => x.Id);

        // 2. Column constraints
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);

        // 3. Unique index — always filter on IsDeleted = 0
        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // 4. Relationships
        builder.HasOne(x => x.Template)
            .WithMany()
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);  // NoAction = no cascade

        // 5. Seed data (system values only)
        builder.HasData(
            new { Id = GenericDeviceStatusIds.Online, ... }
        );
    }
}
```

### EF Core Rules

- **Cascade delete:** Only `Area → Device` uses `Cascade`. All others use `NoAction`.
- **Unique indexes:** Always filter `WHERE IsDeleted = 0` to allow code reuse after soft delete.
- **Enum storage:** Store as string (`.HasConversion<string>()`) for readability.
- **Seed data:** Use `HasData()` with well-known GUIDs. Never use random GUIDs in seed.

### Migration Gotcha

If `HasData` includes a record already manually inserted in the database,
the `database update` will fail with a PRIMARY KEY violation.

**Fix:** Open the generated migration file and remove the `migrationBuilder.InsertData()`
block for the already-existing record. Leave the schema changes intact.

---

## 10. Testing Checklist

For every story, test these scenarios in Swagger before creating a PR:

### Read Endpoints (GET)

```
✓ Returns 200 with data
✓ System values present (isSystem: true)
✓ Custom values present (if any created)
✓ Correct ordering
✓ Empty list returns 200 with [] (not 404)
```

### Create Endpoints (POST)

```
✓ Valid request → 201 Created with new GUID
✓ Missing required fields → 400 with field-specific errors
✓ Invalid code format → 400 with specific message
✓ Duplicate code → 409 Conflict
✓ Operator/Executive role → 403 Forbidden (when auth implemented)
```

### Update Endpoints (PUT)

```
✓ Valid update → 204 No Content
✓ System type → 403 Forbidden
✓ Wrong tenant ID → 403 Forbidden (after auth)
✓ Non-existent ID → 404 Not Found
✓ Invalid input → 400 Bad Request
```

### Delete Endpoints (DELETE)

```
✓ Custom value → 204 No Content
✓ System value → 403 Forbidden
✓ In-use value → 409 Conflict
✓ Non-existent ID → 404 Not Found
✓ Already deleted → 404 Not Found
```

---

## 11. PR & Commit Checklist

### Commit Message Format

```
feat: add device status write operations

Closes #16 - CustomerAdmin can add a custom device status

Commands:
- CreateDeviceStatusCommand + Validator + Handler
  -> Hex color validation (#rrggbb)
  -> Duplicate code check (409)
- UpdateDeviceStatusCommand + Handler
- DeleteDeviceStatusCommand + Handler
  -> In-use check (409)

API:
- POST/PUT/DELETE /api/configuration/device-statuses

Tested:
- POST custom status -> 201 Created
- Duplicate code -> 409 Conflict
- Delete system status -> 403 Forbidden
```

### PR Template

```markdown
## Summary
Implements #XX - [story title]

## Changes
- [list what was added/changed]

## Testing
- [x] [test scenario] -> [expected result]
- [x] [test scenario] -> [expected result]

## Notes
[Any deviations from standard pattern, known issues, or follow-ups]

Closes #XX
```

### Pre-PR Checklist

```
✓ Build succeeds: dotnet build (zero errors)
✓ API runs: dotnet run (no startup exceptions)
✓ All acceptance criteria tested in Swagger
✓ No hardcoded values introduced
✓ All timestamps use DateTime.UtcNow
✓ Soft delete used (not hard delete)
✓ Issue number referenced in commit message
✓ "Closes #XX" in PR body
✓ Story moved to "In Review" on board
```

---

## 12. Common Mistakes & Fixes

### 1. API is still running when building

**Symptom:** `MSB3026: Could not copy ... because it is being used by another process`
**Fix:** Press Ctrl+C to stop the running API, then build.

### 2. `[FromBody] XxxCommand` causes 400 errors

**Symptom:** Swagger returns 400 with `"The command field is required."` or
`"'P' is an invalid start of a value"`
**Fix:** Create a separate `XxxRequest` record in the controller. Map to Command in the action.

### 3. Delete returns 404 instead of 403 for system values

**Symptom:** `DELETE /api/configuration/device-statuses/00000032-...` returns 404
**Root cause:** Query filtered by `TenantId == currentUser.TenantId`, but system values
have `TenantId = null`, so they're not found.
**Fix:** Query without TenantId filter first, then check IsSystem.

### 4. Migration fails with PRIMARY KEY violation

**Symptom:** `dotnet ef database update` fails with `Violation of PRIMARY KEY constraint`
**Root cause:** `HasData()` tries to insert a record that was already manually inserted.
**Fix:** Open the migration file, remove the `migrationBuilder.InsertData()` block for
the conflicting record. Don't remove schema changes.

### 5. `gh: command not found`

**Symptom:** `bash: gh: command not found`
**Fix:**
```bash
export PATH=$PATH:"/c/Program Files/GitHub CLI"
# Permanent fix (already applied):
echo 'export PATH=$PATH:"/c/Program Files/GitHub CLI"' >> ~/.bashrc
```

### 6. Duplicate `using` warning in UpsertTenantLookupOverrideCommand

**Symptom:** `warning CS0105: The using directive for 'EdgePulse.Domain.Entities' appeared previously`
**Root cause:** `sed` command added a duplicate using statement.
**Fix:** Open the file and remove the duplicate `using EdgePulse.Domain.Entities;` line.
This is a warning, not an error — build still succeeds.

### 7. `LookupTypes` not found in Application layer

**Symptom:** `error CS0103: The name 'LookupTypes' does not exist in the current context`
**Root cause:** `LookupTypes.cs` was designed but not created.
**Fix:** Create `EdgePulse.Domain/Constants/LookupTypes.cs` with the string constants.

### 8. Foreign key violation when creating Mill

**Symptom:** `FK constraint violation: FK_Mills_Tenants_TenantId`
**Root cause:** `CurrentUserService` returns `TenantId = 00000099-...` which doesn't exist.
**Fix:** Insert the dev tenant manually in SSMS or via SQL:
```sql
INSERT INTO Tenants (Id, Name, Slug, ContactEmail, Status, CreatedAt, UpdatedAt, IsDeleted)
VALUES ('00000099-0000-0000-0000-000000000001', 'EdgePulse Dev Tenant',
        'edgepulse-dev', 'dev@edgepulse.com', 'Active',
        GETUTCDATE(), GETUTCDATE(), 0);
```

---

## Appendix: File Naming Conventions

```
Commands:    CreateXxxCommand.cs, UpdateXxxCommand.cs, DeleteXxxCommand.cs
Queries:     GetXxxQuery.cs, GetXxxByIdQuery.cs
Controllers: XxxController.cs
Configs:     XxxConfiguration.cs (EF Core)
Migrations:  [Timestamp]_[DescriptiveName].cs

Branch names: feature/US-XXX-short-description
              e.g. feature/US-016-device-status-write
```

## Appendix: Folder Conventions

```
Application/Features/
  Devices/Commands/    → device types, device statuses, metric types, manufacturers
  Devices/Queries/     → same
  Alerts/Commands/     → alert severities, alert statuses
  Alerts/Queries/      → same
  Tenants/Commands/    → tenant management
  Tenants/Queries/
  Mills/Commands/      → mill management
  Mills/Queries/
  Areas/Commands/      → area management
  Areas/Queries/
```

Note: Lookup entities (DeviceType, DeviceStatus, etc.) live under `Features/Devices/`
and `Features/Alerts/` even though they are configuration concerns. This is because
they are contextually related to those domains. The `Features/Configuration/` namespace
was considered but rejected as it would mix unrelated lookup types together.
