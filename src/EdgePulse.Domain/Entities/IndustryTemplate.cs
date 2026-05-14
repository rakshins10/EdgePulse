using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class IndustryTemplate : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsDefault { get; private set; }

    // Navigation properties
    public ICollection<DeviceType> DeviceTypes { get; private set; }
        = new List<DeviceType>();
    public ICollection<DeviceStatus> DeviceStatuses { get; private set; }
        = new List<DeviceStatus>();
    public ICollection<AlertSeverity> AlertSeverities { get; private set; }
        = new List<AlertSeverity>();
    public ICollection<AlertStatus> AlertStatuses { get; private set; }
        = new List<AlertStatus>();
    public ICollection<MetricType> MetricTypes { get; private set; }
        = new List<MetricType>();

    protected IndustryTemplate() { }

    public static IndustryTemplate Create(
        string name,
        string? description = null,
        bool isDefault = false)
    {
        return new IndustryTemplate
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
