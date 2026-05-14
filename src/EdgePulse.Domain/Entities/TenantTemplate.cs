using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class TenantTemplate : BaseEntity
{
    public Guid TenantId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string AssignedBy { get; private set; } = string.Empty;
    public IndustryTemplate? Template { get; private set; }

    protected TenantTemplate() { }

    public static TenantTemplate Create(
        Guid tenantId, Guid templateId, string assignedBy)
    {
        return new TenantTemplate
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateId = templateId,
            AssignedBy = assignedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
