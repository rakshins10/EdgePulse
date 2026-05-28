namespace EdgePulse.Domain.Constants;

/// <summary>
/// Fixed GUIDs for the NordPulp Industries demo tenant.
/// Used by DemoSeedService and referenced in curl demo scripts.
///
/// Prefix convention:
///   10000001 = NordPulp Tenant
///   20000001 = NordPulp Mills
///   30000001 = Lakewood Mill Areas
///   30000002 = Riverside Mill Areas
///   40000001 = Lakewood Mill Devices
///   40000002 = Riverside Mill Devices
///   50000001 = Lakewood Alert Thresholds
///   50000002 = Riverside Alert Thresholds
/// </summary>

public static class DemoTenantIds
{
    public static readonly Guid NordPulp =
        Guid.Parse("10000001-0000-0000-0000-000000000001");
}

public static class DemoMillIds
{
    public static readonly Guid Lakewood =
        Guid.Parse("20000001-0000-0000-0000-000000000001");
    public static readonly Guid Riverside =
        Guid.Parse("20000001-0000-0000-0000-000000000002");
}

public static class DemoAreaIds
{
    // Lakewood Mill
    public static readonly Guid Lakewood_Fiberline =
        Guid.Parse("30000001-0000-0000-0000-000000000001");
    public static readonly Guid Lakewood_Bleaching =
        Guid.Parse("30000001-0000-0000-0000-000000000002");
    public static readonly Guid Lakewood_PaperMachine1 =
        Guid.Parse("30000001-0000-0000-0000-000000000003");
    public static readonly Guid Lakewood_RecoveryBoiler =
        Guid.Parse("30000001-0000-0000-0000-000000000004");

    // Riverside Mill
    public static readonly Guid Riverside_Fiberline =
        Guid.Parse("30000002-0000-0000-0000-000000000001");
    public static readonly Guid Riverside_ChemicalRecovery =
        Guid.Parse("30000002-0000-0000-0000-000000000002");
    public static readonly Guid Riverside_PaperMachine1 =
        Guid.Parse("30000002-0000-0000-0000-000000000003");
    public static readonly Guid Riverside_Utilities =
        Guid.Parse("30000002-0000-0000-0000-000000000004");
}

public static class DemoDeviceIds
{
    // ── Lakewood Mill ────────────────────────────────────────────────────────
    public static readonly Guid LW_FeedWaterPump =
        Guid.Parse("40000001-0000-0000-0000-000000000001");
    public static readonly Guid LW_WhiteLiquorPump =
        Guid.Parse("40000001-0000-0000-0000-000000000002");
    public static readonly Guid LW_ChipFeederMotor =
        Guid.Parse("40000001-0000-0000-0000-000000000003");
    public static readonly Guid LW_ContinuousDigester =
        Guid.Parse("40000001-0000-0000-0000-000000000004");
    public static readonly Guid LW_BleachPump =
        Guid.Parse("40000001-0000-0000-0000-000000000005");
    public static readonly Guid LW_PrimaryRefiner =
        Guid.Parse("40000001-0000-0000-0000-000000000006");
    public static readonly Guid LW_PM1HeadBoxPump =
        Guid.Parse("40000001-0000-0000-0000-000000000007");
    public static readonly Guid LW_PM1DriveMotor =
        Guid.Parse("40000001-0000-0000-0000-000000000008");
    public static readonly Guid LW_RecoveryBoilerFeedPump =
        Guid.Parse("40000001-0000-0000-0000-000000000009");
    public static readonly Guid LW_RecoveryBoilerFanMotor =
        Guid.Parse("40000001-0000-0000-0000-000000000010");

    // ── Riverside Mill ───────────────────────────────────────────────────────
    public static readonly Guid RV_FeedWaterPump =
        Guid.Parse("40000002-0000-0000-0000-000000000001");
    public static readonly Guid RV_ChipFeederMotor =
        Guid.Parse("40000002-0000-0000-0000-000000000002");
    public static readonly Guid RV_BatchDigester =
        Guid.Parse("40000002-0000-0000-0000-000000000003");
    public static readonly Guid RV_BlackLiquorPump =
        Guid.Parse("40000002-0000-0000-0000-000000000004");
    public static readonly Guid RV_GreenLiquorPump =
        Guid.Parse("40000002-0000-0000-0000-000000000005");
    public static readonly Guid RV_RecoveryFanMotor =
        Guid.Parse("40000002-0000-0000-0000-000000000006");
    public static readonly Guid RV_PrimaryRefiner =
        Guid.Parse("40000002-0000-0000-0000-000000000007");
    public static readonly Guid RV_PM1WhiteWaterPump =
        Guid.Parse("40000002-0000-0000-0000-000000000008");
    public static readonly Guid RV_CoolingWaterPump =
        Guid.Parse("40000002-0000-0000-0000-000000000009");
    public static readonly Guid RV_MainDriveMotor =
        Guid.Parse("40000002-0000-0000-0000-000000000010");
}

public static class DemoThresholdIds
{
    // ── Lakewood thresholds ──────────────────────────────────────────────────
    // Feed Water Pump
    public static readonly Guid LW_FeedWaterPump_BearingTempHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000001");
    public static readonly Guid LW_FeedWaterPump_BearingTempCritical =
        Guid.Parse("50000001-0000-0000-0000-000000000002");
    public static readonly Guid LW_FeedWaterPump_VibrationHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000003");
    public static readonly Guid LW_FeedWaterPump_FlowLow =
        Guid.Parse("50000001-0000-0000-0000-000000000004");
    // Continuous Digester
    public static readonly Guid LW_Digester_PressureHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000005");
    public static readonly Guid LW_Digester_PressureCritical =
        Guid.Parse("50000001-0000-0000-0000-000000000006");
    public static readonly Guid LW_Digester_TempHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000007");
    // Chip Feeder Motor
    public static readonly Guid LW_ChipFeeder_TempHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000008");
    public static readonly Guid LW_ChipFeeder_VibrationHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000009");
    // Primary Refiner
    public static readonly Guid LW_Refiner_PlateGapLow =
        Guid.Parse("50000001-0000-0000-0000-000000000010");
    public static readonly Guid LW_Refiner_MotorTempHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000011");
    // PM1 Drive Motor
    public static readonly Guid LW_PM1Motor_WindingTempHigh =
        Guid.Parse("50000001-0000-0000-0000-000000000012");
    public static readonly Guid LW_PM1Motor_VibrationCritical =
        Guid.Parse("50000001-0000-0000-0000-000000000013");

    // ── Riverside thresholds ─────────────────────────────────────────────────
    // Feed Water Pump
    public static readonly Guid RV_FeedWaterPump_BearingTempHigh =
        Guid.Parse("50000002-0000-0000-0000-000000000001");
    public static readonly Guid RV_FeedWaterPump_VibrationHigh =
        Guid.Parse("50000002-0000-0000-0000-000000000002");
    // Batch Digester
    public static readonly Guid RV_Digester_PressureCritical =
        Guid.Parse("50000002-0000-0000-0000-000000000003");
    public static readonly Guid RV_Digester_TempHigh =
        Guid.Parse("50000002-0000-0000-0000-000000000004");
    // Black Liquor Pump
    public static readonly Guid RV_BlackLiquorPump_TempHigh =
        Guid.Parse("50000002-0000-0000-0000-000000000005");
    public static readonly Guid RV_BlackLiquorPump_FlowLow =
        Guid.Parse("50000002-0000-0000-0000-000000000006");
    // Main Drive Motor
    public static readonly Guid RV_MainDriveMotor_WindingTempCritical =
        Guid.Parse("50000002-0000-0000-0000-000000000007");
    public static readonly Guid RV_MainDriveMotor_VibrationHigh =
        Guid.Parse("50000002-0000-0000-0000-000000000008");
}
