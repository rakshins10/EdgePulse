using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class DeviceModel : LookupBaseEntity
{
    public Guid ManufacturerId { get; private set; }
    public string? ModelNumber { get; private set; }
    public string? Specifications { get; private set; }
    public DeviceManufacturer? Manufacturer { get; private set; }

    protected DeviceModel() { }

    public static DeviceModel CreateSystemValue(
        Guid id, Guid templateId, Guid manufacturerId,
        string name, string code, string? modelNumber = null,
        string? specifications = null, int sortOrder = 0)
    {
        return new DeviceModel
        {
            Id = id, TemplateId = templateId, TenantId = null,
            ManufacturerId = manufacturerId, Name = name, Code = code,
            ModelNumber = modelNumber, Specifications = specifications,
            IsSystem = true, IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public static DeviceModel CreateCustomValue(
        Guid tenantId, Guid manufacturerId, string name, string code,
        string? modelNumber = null, string? specifications = null,
        int sortOrder = 0)
    {
        return new DeviceModel
        {
            Id = Guid.NewGuid(), TemplateId = null, TenantId = tenantId,
            ManufacturerId = manufacturerId, Name = name,
            Code = code.ToUpperInvariant(), ModelNumber = modelNumber,
            Specifications = specifications, IsSystem = false,
            IsActive = true, SortOrder = sortOrder,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }
}
