using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(20);
        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityDisplay).HasMaxLength(300);
        // property diffs as JSON; nvarchar(max)
        builder.Property(x => x.ChangesJson);

        builder.HasIndex(x => new { x.TenantId, x.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Tenant_Timestamp");
        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");
    }
}
