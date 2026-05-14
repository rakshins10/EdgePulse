namespace EdgePulse.Domain.Common;

public abstract class TenantBaseEntity : BaseEntity
{
    public Guid TenantId { get; protected set; }
}
