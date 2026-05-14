using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class Device : TenantBaseEntity
{
    public Guid MillId { get; private set; }
    public Guid AreaId { get; private set; }
    public Guid TypeId { get; private set; }
    public Guid StatusId { get; private set; }
    public Guid? ManufacturerId { get; private set; }
    public Guid? ModelId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? SerialNumber { get; private set; }
    public DateOnly? InstallDate { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public string? Description { get; private set; }
    public Mill? Mill { get; private set; }
    public Area? Area { get; private set; }
    public DeviceType? Type { get; private set; }
    public DeviceStatus? Status { get; private set; }
    public DeviceManufacturer? Manufacturer { get; private set; }
    public DeviceModel? Model { get; private set; }
    public ICollection<Attachment> Attachments { get; private set; }
        = new List<Attachment>();

    protected Device() { }

    public static Device Create(
        Guid tenantId, Guid millId, Guid areaId,
        Guid typeId, Guid statusId, string name, string code,
        string? serialNumber = null, DateOnly? installDate = null,
        Guid? manufacturerId = null, Guid? modelId = null,
        string? description = null)
    {
        return new Device
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            MillId = millId, AreaId = areaId,
            TypeId = typeId, StatusId = statusId,
            Name = name, Code = code.ToUpperInvariant(),
            SerialNumber = serialNumber, InstallDate = installDate,
            ManufacturerId = manufacturerId, ModelId = modelId,
            Description = description,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateStatus(Guid statusId)
    {
        StatusId = statusId;
        MarkAsUpdated();
    }

    public void UpdateLastSeen()
    {
        LastSeenAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void UpdateDetails(string name, Guid areaId,
        Guid typeId, Guid? manufacturerId, Guid? modelId,
        string? serialNumber, DateOnly? installDate,
        string? description)
    {
        Name = name; AreaId = areaId; TypeId = typeId;
        ManufacturerId = manufacturerId; ModelId = modelId;
        SerialNumber = serialNumber; InstallDate = installDate;
        Description = description;
        MarkAsUpdated();
    }

    public void Decommission(Guid decommissionedStatusId)
    {
        StatusId = decommissionedStatusId;
        MarkAsDeleted();
    }
}
