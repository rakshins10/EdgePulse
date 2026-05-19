namespace EdgePulse.Domain.Constants;

/// <summary>
/// String constants for lookup type discrimination.
/// Used in TenantLookupOverrides to identify which
/// lookup table is being overridden.
/// These are internal technical identifiers -- not shown to users.
/// </summary>
public static class LookupTypes
{
    public const string DeviceType         = "DeviceType";
    public const string DeviceStatus       = "DeviceStatus";
    public const string AlertSeverity      = "AlertSeverity";
    public const string AlertStatus        = "AlertStatus";
    public const string MetricType         = "MetricType";
    public const string Unit               = "Unit";
    public const string MaintenanceType    = "MaintenanceType";
    public const string LocationType       = "LocationType";
    public const string DeviceManufacturer = "DeviceManufacturer";
    public const string DeviceModel        = "DeviceModel";
}
