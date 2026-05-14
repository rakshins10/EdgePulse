using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class Area : TenantBaseEntity
{
    public Guid MillId { get; private set; }
    public Guid? LocationTypeId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Mill? Mill { get; private set; }
    public LocationType? LocationType { get; private set; }
    public ICollection<Device> Devices { get; private set; }
        = new List<Device>();

    protected Area() { }

    public static Area Create(
        Guid tenantId, Guid millId, string name, string code,
        Guid? locationTypeId = null, string? description = null)
    {
        return new Area
        {
            Id = Guid.NewGuid(), TenantId = tenantId,
            MillId = millId, Name = name,
            Code = code.ToUpperInvariant(),
            LocationTypeId = locationTypeId,
            Description = description,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name,
        string? description, Guid? locationTypeId)
    {
        Name = name; Description = description;
        LocationTypeId = locationTypeId;
        MarkAsUpdated();
    }
}
