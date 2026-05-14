namespace EdgePulse.Domain.Constants;

/// <summary>
/// Fixed GUIDs for system-seeded lookup values.
/// These are referenced in code by name -- never by string or integer.
/// Prefix convention:
///   00000010 = Industry Template IDs
///   00000011 = Pulp and Paper DeviceType IDs
///   00000012 = Pulp and Paper DeviceStatus IDs
///   00000021 = Manufacturing DeviceType IDs
///   00000031 = Generic DeviceType IDs
///   00000032 = Generic DeviceStatus IDs
///   00000033 = Generic AlertSeverity IDs
///   00000034 = Generic AlertStatus IDs
/// </summary>

public static class IndustryTemplateIds
{
    public static readonly Guid PulpAndPaper =
        Guid.Parse("00000010-0000-0000-0000-000000000001");
    public static readonly Guid Manufacturing =
        Guid.Parse("00000010-0000-0000-0000-000000000002");
    public static readonly Guid Generic =
        Guid.Parse("00000010-0000-0000-0000-000000000003");
}

public static class PulpAndPaperDeviceTypeIds
{
    public static readonly Guid Pump =
        Guid.Parse("00000011-0000-0000-0000-000000000001");
    public static readonly Guid Motor =
        Guid.Parse("00000011-0000-0000-0000-000000000002");
    public static readonly Guid Valve =
        Guid.Parse("00000011-0000-0000-0000-000000000003");
    public static readonly Guid Digester =
        Guid.Parse("00000011-0000-0000-0000-000000000004");
    public static readonly Guid ChipFeeder =
        Guid.Parse("00000011-0000-0000-0000-000000000005");
    public static readonly Guid Pulper =
        Guid.Parse("00000011-0000-0000-0000-000000000006");
    public static readonly Guid Refiner =
        Guid.Parse("00000011-0000-0000-0000-000000000007");
}

public static class ManufacturingDeviceTypeIds
{
    public static readonly Guid CncMachine =
        Guid.Parse("00000021-0000-0000-0000-000000000001");
    public static readonly Guid RobotArm =
        Guid.Parse("00000021-0000-0000-0000-000000000002");
    public static readonly Guid Conveyor =
        Guid.Parse("00000021-0000-0000-0000-000000000003");
    public static readonly Guid Press =
        Guid.Parse("00000021-0000-0000-0000-000000000004");
    public static readonly Guid Lathe =
        Guid.Parse("00000021-0000-0000-0000-000000000005");
    public static readonly Guid WeldingStation =
        Guid.Parse("00000021-0000-0000-0000-000000000006");
}

public static class GenericDeviceTypeIds
{
    public static readonly Guid Sensor =
        Guid.Parse("00000031-0000-0000-0000-000000000001");
    public static readonly Guid Controller =
        Guid.Parse("00000031-0000-0000-0000-000000000002");
    public static readonly Guid Actuator =
        Guid.Parse("00000031-0000-0000-0000-000000000003");
    public static readonly Guid Motor =
        Guid.Parse("00000031-0000-0000-0000-000000000004");
    public static readonly Guid Pump =
        Guid.Parse("00000031-0000-0000-0000-000000000005");
}

public static class GenericDeviceStatusIds
{
    public static readonly Guid Online =
        Guid.Parse("00000032-0000-0000-0000-000000000001");
    public static readonly Guid Offline =
        Guid.Parse("00000032-0000-0000-0000-000000000002");
    public static readonly Guid Maintenance =
        Guid.Parse("00000032-0000-0000-0000-000000000003");
    public static readonly Guid Decommissioned =
        Guid.Parse("00000032-0000-0000-0000-000000000004");
}

public static class GenericAlertSeverityIds
{
    public static readonly Guid Critical =
        Guid.Parse("00000033-0000-0000-0000-000000000001");
    public static readonly Guid High =
        Guid.Parse("00000033-0000-0000-0000-000000000002");
    public static readonly Guid Medium =
        Guid.Parse("00000033-0000-0000-0000-000000000003");
    public static readonly Guid Low =
        Guid.Parse("00000033-0000-0000-0000-000000000004");
}

public static class GenericAlertStatusIds
{
    public static readonly Guid Open =
        Guid.Parse("00000034-0000-0000-0000-000000000001");
    public static readonly Guid Acknowledged =
        Guid.Parse("00000034-0000-0000-0000-000000000002");
    public static readonly Guid Assigned =
        Guid.Parse("00000034-0000-0000-0000-000000000003");
    public static readonly Guid Resolved =
        Guid.Parse("00000034-0000-0000-0000-000000000004");
    public static readonly Guid Closed =
        Guid.Parse("00000034-0000-0000-0000-000000000005");
}
