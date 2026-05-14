namespace EdgePulse.Domain.Common;

public abstract class LookupBaseEntity : BaseEntity
{
    // TemplateId set + TenantId null = template value
    // TemplateId null + TenantId set = tenant custom value
    public Guid? TemplateId { get; protected set; }
    public Guid? TenantId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    public string? Description { get; protected set; }
    public bool IsSystem { get; protected set; }
    // System values cannot be deleted
    public bool IsActive { get; protected set; } = true;
    public int SortOrder { get; protected set; }

    public void Deactivate()
    {
        if (IsSystem)
            throw new InvalidOperationException(
                "System lookup values cannot be deactivated directly. " +
                "Use TenantLookupOverride instead.");
        IsActive = false;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void UpdateDetails(string name, string? description)
    {
        if (IsSystem)
            throw new InvalidOperationException(
                "System lookup values cannot be modified directly. " +
                "Use TenantLookupOverride to rename.");
        Name = name;
        Description = description;
        MarkAsUpdated();
    }
}
