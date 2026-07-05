#pragma warning disable CS8625 // null literal in non-nullable context (SQL params)
#pragma warning disable CS8604 // possible null reference argument (SQL params)
using EdgePulse.Domain.Constants;
using EdgePulse.Domain.Entities;
using EdgePulse.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EdgePulse.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the NordPulp Industries demo tenant with 2 mills, 8 areas,
/// 20 devices, and 21 alert thresholds.
///
/// All IDs are fixed (see DemoIds.cs) so curl demo scripts work
/// with the same IDs every time.
///
/// Idempotent: safe to run multiple times — checks existence before inserting.
/// Run via: dotnet run --project src/backend/EdgePulse.API -- --seed
/// </summary>
public class DemoSeedService
{
    private readonly EdgePulseDbContext _db;
    private readonly ILogger<DemoSeedService> _logger;

    // Fixed timestamps for seeded entities
    private static readonly DateTime SeedDate =
        new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public DemoSeedService(
        EdgePulseDbContext db,
        ILogger<DemoSeedService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting NordPulp Industries demo seed...");

        await SeedTenantAsync(ct);
        await SeedMillsAsync(ct);
        await SeedAreasAsync(ct);
        await SeedDevicesAsync(ct);
        await SeedAlertThresholdsAsync(ct);

        _logger.LogInformation("Demo seed complete.");
    }

    // ─── Tenant ──────────────────────────────────────────────────────────────

    private async Task SeedTenantAsync(CancellationToken ct)
    {
        // Already seeded with the correct ID — nothing to do
        if (await _db.Tenants.AnyAsync(t => t.Id == DemoTenantIds.NordPulp, ct))
        {
            _logger.LogInformation("Tenant NordPulp already exists with correct ID — skipping.");
            return;
        }

        // A NordPulp tenant exists with a DIFFERENT ID (from a previous manual seed).
        // Remove it (cascade-deletes mills, areas, devices, thresholds, alerts)
        // so we can re-insert with the fixed demo ID.
        var existingId = await _db.Tenants
            .Where(t => t.Slug == "nordpulp")
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        if (existingId.HasValue)
        {
            _logger.LogWarning(
                "Found existing nordpulp tenant with ID {Id} (expected {ExpectedId}). " +
                "Removing it so demo IDs are consistent.",
                existingId.Value, DemoTenantIds.NordPulp);

            // Raw DELETE is safe here — all child tables cascade from Tenants
            await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM Tenants WHERE Id = {0}",
                parameters: new object[] { existingId.Value },
                cancellationToken: ct);
        }

        await _db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO Tenants (Id, Name, Slug, ContactEmail, Status,
                                 IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
            VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})
            """,
            DemoTenantIds.NordPulp, "NordPulp Industries", "nordpulp",
            "ops@nordpulp.example", "Active",
            false, (DateTime?)null, SeedDate, SeedDate);

        _logger.LogInformation("Seeded tenant: NordPulp Industries ({Id})", DemoTenantIds.NordPulp);
    }

    // ─── Mills ───────────────────────────────────────────────────────────────

    private async Task SeedMillsAsync(CancellationToken ct)
    {
        var mills = new[]
        {
            new
            {
                Id = DemoMillIds.Lakewood,
                TenantId = DemoTenantIds.NordPulp,
                Name = "Lakewood Mill",
                Code = "LW",
                Location = "Lakewood, Finland",
                Timezone = "Europe/Helsinki",
                HasInternet = true,
                DeploymentMode = (int)DeploymentMode.Cloud
            },
            new
            {
                Id = DemoMillIds.Riverside,
                TenantId = DemoTenantIds.NordPulp,
                Name = "Riverside Mill",
                Code = "RV",
                Location = "Riverside, Sweden",
                Timezone = "Europe/Stockholm",
                HasInternet = true,
                DeploymentMode = (int)DeploymentMode.Cloud
            },
        };

        foreach (var mill in mills)
        {
            if (await _db.Mills.AnyAsync(m => m.Id == mill.Id, ct)) continue;

            await _db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Mills (Id, TenantId, Name, Code, Location, Timezone,
                                   HasInternet, DeploymentMode,
                                   IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
                VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11})
                """,
                mill.Id, mill.TenantId, mill.Name, mill.Code,
                mill.Location, mill.Timezone, mill.HasInternet, mill.DeploymentMode,
                false, (DateTime?)null, SeedDate, SeedDate);

            _logger.LogInformation("Seeded mill: {Name} ({Id})", mill.Name, mill.Id);
        }
    }

    // ─── Areas ───────────────────────────────────────────────────────────────

    private async Task SeedAreasAsync(CancellationToken ct)
    {
        var areas = new[]
        {
            // Lakewood Mill areas
            (DemoAreaIds.Lakewood_Fiberline,      DemoMillIds.Lakewood, "Fiberline",         "LW-FL"),
            (DemoAreaIds.Lakewood_Bleaching,       DemoMillIds.Lakewood, "Bleaching",          "LW-BL"),
            (DemoAreaIds.Lakewood_PaperMachine1,   DemoMillIds.Lakewood, "Paper Machine 1",    "LW-PM1"),
            (DemoAreaIds.Lakewood_RecoveryBoiler,  DemoMillIds.Lakewood, "Recovery Boiler",    "LW-RB"),
            // Riverside Mill areas
            (DemoAreaIds.Riverside_Fiberline,         DemoMillIds.Riverside, "Fiberline",           "RV-FL"),
            (DemoAreaIds.Riverside_ChemicalRecovery,  DemoMillIds.Riverside, "Chemical Recovery",   "RV-CR"),
            (DemoAreaIds.Riverside_PaperMachine1,     DemoMillIds.Riverside, "Paper Machine 1",     "RV-PM1"),
            (DemoAreaIds.Riverside_Utilities,         DemoMillIds.Riverside, "Utilities",           "RV-UT"),
        };

        foreach (var (id, millId, name, code) in areas)
        {
            if (await _db.Areas.AnyAsync(a => a.Id == id, ct)) continue;

            await _db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Areas (Id, TenantId, MillId, Name, Code,
                                   LocationTypeId, Description,
                                   IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
                VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10})
                """,
                id, DemoTenantIds.NordPulp, millId, name, code,
                (Guid?)null, (string?)null,
                false, (DateTime?)null, SeedDate, SeedDate);

            _logger.LogInformation("Seeded area: {Name} ({Id})", name, id);
        }
    }

    // ─── Devices ─────────────────────────────────────────────────────────────

    private async Task SeedDevicesAsync(CancellationToken ct)
    {
        // (id, areaId, millId, typeId, name, code)
        var devices = new[]
        {
            // ── Lakewood: Fiberline ───────────────────────────────────────────
            (DemoDeviceIds.LW_FeedWaterPump,
             DemoAreaIds.Lakewood_Fiberline, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Pump,
             "Feed Water Pump", "PUMP-LW-001"),

            (DemoDeviceIds.LW_WhiteLiquorPump,
             DemoAreaIds.Lakewood_Fiberline, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Pump,
             "White Liquor Pump", "PUMP-LW-002"),

            (DemoDeviceIds.LW_ChipFeederMotor,
             DemoAreaIds.Lakewood_Fiberline, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Motor,
             "Chip Feeder Motor", "MOTOR-LW-001"),

            (DemoDeviceIds.LW_ContinuousDigester,
             DemoAreaIds.Lakewood_Fiberline, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Digester,
             "Continuous Digester", "DGST-LW-001"),

            // ── Lakewood: Bleaching ───────────────────────────────────────────
            (DemoDeviceIds.LW_BleachPump,
             DemoAreaIds.Lakewood_Bleaching, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Pump,
             "Bleach Pump", "PUMP-LW-003"),

            // ── Lakewood: Paper Machine 1 ─────────────────────────────────────
            (DemoDeviceIds.LW_PrimaryRefiner,
             DemoAreaIds.Lakewood_PaperMachine1, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Refiner,
             "Primary Refiner", "RFNR-LW-001"),

            (DemoDeviceIds.LW_PM1HeadBoxPump,
             DemoAreaIds.Lakewood_PaperMachine1, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Pump,
             "PM1 Head Box Pump", "PUMP-LW-004"),

            (DemoDeviceIds.LW_PM1DriveMotor,
             DemoAreaIds.Lakewood_PaperMachine1, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Motor,
             "PM1 Drive Motor", "MOTOR-LW-002"),

            // ── Lakewood: Recovery Boiler ─────────────────────────────────────
            (DemoDeviceIds.LW_RecoveryBoilerFeedPump,
             DemoAreaIds.Lakewood_RecoveryBoiler, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Pump,
             "Recovery Boiler Feed Pump", "PUMP-LW-005"),

            (DemoDeviceIds.LW_RecoveryBoilerFanMotor,
             DemoAreaIds.Lakewood_RecoveryBoiler, DemoMillIds.Lakewood,
             PulpAndPaperDeviceTypeIds.Motor,
             "Recovery Boiler Fan Motor", "MOTOR-LW-003"),

            // ── Riverside: Fiberline ──────────────────────────────────────────
            (DemoDeviceIds.RV_FeedWaterPump,
             DemoAreaIds.Riverside_Fiberline, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Pump,
             "Feed Water Pump", "PUMP-RV-001"),

            (DemoDeviceIds.RV_ChipFeederMotor,
             DemoAreaIds.Riverside_Fiberline, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Motor,
             "Chip Feeder Motor", "MOTOR-RV-001"),

            (DemoDeviceIds.RV_BatchDigester,
             DemoAreaIds.Riverside_Fiberline, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Digester,
             "Batch Digester", "DGST-RV-001"),

            // ── Riverside: Chemical Recovery ──────────────────────────────────
            (DemoDeviceIds.RV_BlackLiquorPump,
             DemoAreaIds.Riverside_ChemicalRecovery, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Pump,
             "Black Liquor Pump", "PUMP-RV-002"),

            (DemoDeviceIds.RV_GreenLiquorPump,
             DemoAreaIds.Riverside_ChemicalRecovery, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Pump,
             "Green Liquor Pump", "PUMP-RV-003"),

            (DemoDeviceIds.RV_RecoveryFanMotor,
             DemoAreaIds.Riverside_ChemicalRecovery, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Motor,
             "Recovery Fan Motor", "MOTOR-RV-002"),

            // ── Riverside: Paper Machine 1 ────────────────────────────────────
            (DemoDeviceIds.RV_PrimaryRefiner,
             DemoAreaIds.Riverside_PaperMachine1, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Refiner,
             "Primary Refiner", "RFNR-RV-001"),

            (DemoDeviceIds.RV_PM1WhiteWaterPump,
             DemoAreaIds.Riverside_PaperMachine1, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Pump,
             "PM1 White Water Pump", "PUMP-RV-004"),

            // ── Riverside: Utilities ──────────────────────────────────────────
            (DemoDeviceIds.RV_CoolingWaterPump,
             DemoAreaIds.Riverside_Utilities, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Pump,
             "Cooling Water Pump", "PUMP-RV-005"),

            (DemoDeviceIds.RV_MainDriveMotor,
             DemoAreaIds.Riverside_Utilities, DemoMillIds.Riverside,
             PulpAndPaperDeviceTypeIds.Motor,
             "Main Drive Motor", "MOTOR-RV-003"),
        };

        foreach (var (id, areaId, millId, typeId, name, code) in devices)
        {
            if (await _db.Devices.AnyAsync(d => d.Id == id, ct)) continue;

            await _db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO Devices
                    (Id, TenantId, MillId, AreaId, TypeId, StatusId,
                     ManufacturerId, ModelId, Name, Code,
                     SerialNumber, InstallDate, LastSeenAt, Description,
                     IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
                VALUES ({0},{1},{2},{3},{4},{5},
                        {6},{7},{8},{9},
                        {10},{11},{12},{13},
                        {14},{15},{16},{17})
                """,
                id, DemoTenantIds.NordPulp, millId, areaId,
                typeId, GenericDeviceStatusIds.Online,
                (Guid?)null, (Guid?)null, name, code,
                (string?)null, (DateOnly?)null, (DateTime?)null, (string?)null,
                false, (DateTime?)null, SeedDate, SeedDate);

            _logger.LogInformation("Seeded device: {Code} — {Name}", code, name);
        }
    }

    // ─── Alert Thresholds ─────────────────────────────────────────────────────

    private async Task SeedAlertThresholdsAsync(CancellationToken ct)
    {
        // (id, deviceId, metricKey, name, minValue, maxValue, severityCode, unit, consecutiveCount)
        var thresholds = new[]
        {
            // ── Lakewood: Feed Water Pump ─────────────────────────────────────
            (DemoThresholdIds.LW_FeedWaterPump_BearingTempHigh,
             DemoDeviceIds.LW_FeedWaterPump,
             "bearing_temp", "Bearing Temperature High",
             (double?)null, (double?)75.0, "HIGH", "°C", 3),

            (DemoThresholdIds.LW_FeedWaterPump_BearingTempCritical,
             DemoDeviceIds.LW_FeedWaterPump,
             "bearing_temp", "Bearing Temperature Critical",
             (double?)null, (double?)85.0, "CRITICAL", "°C", 2),

            (DemoThresholdIds.LW_FeedWaterPump_VibrationHigh,
             DemoDeviceIds.LW_FeedWaterPump,
             "vibration", "Pump Vibration High",
             (double?)null, (double?)8.0, "HIGH", "mm/s", 3),

            (DemoThresholdIds.LW_FeedWaterPump_FlowLow,
             DemoDeviceIds.LW_FeedWaterPump,
             "flow_rate", "Feed Flow Rate Low",
             (double?)20.0, (double?)null, "CRITICAL", "m³/h", 3),

            // ── Lakewood: Continuous Digester ─────────────────────────────────
            (DemoThresholdIds.LW_Digester_PressureHigh,
             DemoDeviceIds.LW_ContinuousDigester,
             "pressure", "Digester Pressure High",
             (double?)null, (double?)7.5, "HIGH", "bar", 3),

            (DemoThresholdIds.LW_Digester_PressureCritical,
             DemoDeviceIds.LW_ContinuousDigester,
             "pressure", "Digester Pressure Critical",
             (double?)null, (double?)8.0, "CRITICAL", "bar", 2),

            (DemoThresholdIds.LW_Digester_TempHigh,
             DemoDeviceIds.LW_ContinuousDigester,
             "temperature", "Digester Temperature High",
             (double?)null, (double?)180.0, "HIGH", "°C", 2),

            // ── Lakewood: Chip Feeder Motor ───────────────────────────────────
            (DemoThresholdIds.LW_ChipFeeder_TempHigh,
             DemoDeviceIds.LW_ChipFeederMotor,
             "winding_temp", "Winding Temperature High",
             (double?)null, (double?)105.0, "CRITICAL", "°C", 2),

            (DemoThresholdIds.LW_ChipFeeder_VibrationHigh,
             DemoDeviceIds.LW_ChipFeederMotor,
             "vibration", "Motor Vibration High",
             (double?)null, (double?)10.0, "HIGH", "mm/s", 3),

            // ── Lakewood: Primary Refiner ─────────────────────────────────────
            (DemoThresholdIds.LW_Refiner_PlateGapLow,
             DemoDeviceIds.LW_PrimaryRefiner,
             "plate_gap", "Refiner Plate Gap Critical",
             (double?)0.02, (double?)null, "CRITICAL", "mm", 3),

            (DemoThresholdIds.LW_Refiner_MotorTempHigh,
             DemoDeviceIds.LW_PrimaryRefiner,
             "motor_temp", "Refiner Motor Temperature High",
             (double?)null, (double?)95.0, "HIGH", "°C", 3),

            // ── Lakewood: PM1 Drive Motor ─────────────────────────────────────
            (DemoThresholdIds.LW_PM1Motor_WindingTempHigh,
             DemoDeviceIds.LW_PM1DriveMotor,
             "winding_temp", "PM1 Winding Temperature High",
             (double?)null, (double?)100.0, "HIGH", "°C", 3),

            (DemoThresholdIds.LW_PM1Motor_VibrationCritical,
             DemoDeviceIds.LW_PM1DriveMotor,
             "vibration", "PM1 Motor Vibration Critical",
             (double?)null, (double?)12.0, "CRITICAL", "mm/s", 2),

            // ── Riverside: Feed Water Pump ────────────────────────────────────
            (DemoThresholdIds.RV_FeedWaterPump_BearingTempHigh,
             DemoDeviceIds.RV_FeedWaterPump,
             "bearing_temp", "Bearing Temperature High",
             (double?)null, (double?)80.0, "HIGH", "°C", 3),

            (DemoThresholdIds.RV_FeedWaterPump_VibrationHigh,
             DemoDeviceIds.RV_FeedWaterPump,
             "vibration", "Pump Vibration High",
             (double?)null, (double?)8.0, "HIGH", "mm/s", 3),

            // ── Riverside: Batch Digester ─────────────────────────────────────
            (DemoThresholdIds.RV_Digester_PressureCritical,
             DemoDeviceIds.RV_BatchDigester,
             "pressure", "Digester Pressure Critical",
             (double?)null, (double?)8.0, "CRITICAL", "bar", 2),

            (DemoThresholdIds.RV_Digester_TempHigh,
             DemoDeviceIds.RV_BatchDigester,
             "temperature", "Digester Temperature High",
             (double?)null, (double?)178.0, "HIGH", "°C", 2),

            // ── Riverside: Black Liquor Pump ──────────────────────────────────
            (DemoThresholdIds.RV_BlackLiquorPump_TempHigh,
             DemoDeviceIds.RV_BlackLiquorPump,
             "pump_temp", "Black Liquor Pump Temperature High",
             (double?)null, (double?)90.0, "HIGH", "°C", 3),

            (DemoThresholdIds.RV_BlackLiquorPump_FlowLow,
             DemoDeviceIds.RV_BlackLiquorPump,
             "flow_rate", "Black Liquor Flow Low",
             (double?)15.0, (double?)null, "HIGH", "m³/h", 3),

            // ── Riverside: Main Drive Motor ───────────────────────────────────
            (DemoThresholdIds.RV_MainDriveMotor_WindingTempCritical,
             DemoDeviceIds.RV_MainDriveMotor,
             "winding_temp", "Main Drive Winding Temperature Critical",
             (double?)null, (double?)110.0, "CRITICAL", "°C", 2),

            (DemoThresholdIds.RV_MainDriveMotor_VibrationHigh,
             DemoDeviceIds.RV_MainDriveMotor,
             "vibration", "Main Drive Motor Vibration High",
             (double?)null, (double?)9.0, "HIGH", "mm/s", 3),
        };

        foreach (var (id, deviceId, metricKey, name,
                      minVal, maxVal, severityCode, unit, consecutive)
                 in thresholds)
        {
            if (await _db.AlertThresholds.AnyAsync(t => t.Id == id, ct)) continue;

            await _db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO AlertThresholds
                    (Id, TenantId, DeviceId, MetricKey, Name,
                     MinValue, MaxValue, Unit, SeverityCode,
                     ConsecutiveCount, IsActive, Description,
                     IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
                VALUES ({0},{1},{2},{3},{4},
                        {5},{6},{7},{8},
                        {9},{10},{11},
                        {12},{13},{14},{15})
                """,
                id, DemoTenantIds.NordPulp, deviceId, metricKey, name,
                minVal, maxVal, unit, severityCode,
                consecutive, true, (string?)null,
                false, (DateTime?)null, SeedDate, SeedDate);

            _logger.LogInformation(
                "Seeded threshold: [{Severity}] {Name} on {DeviceId}",
                severityCode, name, deviceId);
        }
    }
}
