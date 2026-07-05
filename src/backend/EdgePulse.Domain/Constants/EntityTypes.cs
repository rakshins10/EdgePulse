namespace EdgePulse.Domain.Constants;

/// <summary>
/// Entity type identifiers used in Attachments table.
/// Keeps EntityType column consistent -- no magic strings.
/// </summary>
public static class EntityTypes
{
    public const string Device              = "Device";
    public const string DeviceModel         = "DeviceModel";
    public const string DeviceManufacturer  = "DeviceManufacturer";
    public const string Mill                = "Mill";
    public const string Area                = "Area";
    public const string Alert               = "Alert";
    public const string MaintenanceRecord   = "MaintenanceRecord";
    public const string UserProfile         = "UserProfile";
    public const string Tenant              = "Tenant";
    public const string IndustryTemplate    = "IndustryTemplate";
}
