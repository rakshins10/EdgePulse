using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Number).IsUnique();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Priority).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.AssignedTo).HasMaxLength(200);
        builder.Property(x => x.PartsUsed).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(x => x.CompletedBy).HasMaxLength(200);
        builder.Property(x => x.CompletionNotes).HasMaxLength(2000);

        // Work-order board queries: tenant + status; device history: device + date
        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_WorkOrders_Tenant_Status");
        builder.HasIndex(x => new { x.DeviceId, x.CreatedAt })
            .HasDatabaseName("IX_WorkOrders_Device_CreatedAt");
    }
}
