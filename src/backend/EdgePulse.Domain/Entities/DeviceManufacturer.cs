using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class DeviceManufacturer : LookupBaseEntity
{
    public string? Website { get; private set; }
    public string? Country { get; private set; }
    public ICollection<DeviceModel> DeviceModels { get; private set; }
        = new List<DeviceModel>();

    protected DeviceManufacturer() { }

    public static DeviceManufacturer CreateSystemValue(
        Guid id, Guid templateId, string name, string code,
        string? website = null, string? country = null, int sortOrder = 0)
    {
        return new DeviceManufacturer
        {
            Id = id, TemplateId = templateId, TenantId = null,
            Name = name, Code = code, Website = website,
            Country = country, IsSystem = true, IsActive = true,
            SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public static DeviceManufacturer CreateCustomValue(
        Guid tenantId, string name, string code,
        string? website = null, string? country = null, int sortOrder = 0)
    {
        return new DeviceManufacturer
        {
            Id = Guid.NewGuid(), TemplateId = null, TenantId = tenantId,
            Name = name, Code = code.ToUpperInvariant(),
            Website = website, Country = country,
            IsSystem = false, IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
