using EdgePulse.Domain.Common;

namespace EdgePulse.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Active";
    public ICollection<Mill> Mills { get; private set; }
        = new List<Mill>();
    public TenantTemplate? TenantTemplate { get; private set; }

    protected Tenant() { }

    public static Tenant Create(
        string name, string slug, string contactEmail)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(), Name = name,
            Slug = slug.ToLowerInvariant(),
            ContactEmail = contactEmail, Status = "Active",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
    }

    public void Suspend() { Status = "Suspended"; MarkAsUpdated(); }
    public void Activate() { Status = "Active"; MarkAsUpdated(); }

    public void UpdateDetails(string name, string contactEmail)
    {
        Name = name;
        ContactEmail = contactEmail;
        MarkAsUpdated();
    }
}
