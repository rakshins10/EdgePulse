using EdgePulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EdgePulse.Infrastructure.Persistence.Configurations;

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        builder.ToTable("Alerts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MetricKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SeverityCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.StatusCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Unit)
            .HasMaxLength(20);

        builder.Property(x => x.AcknowledgedBy)
            .HasMaxLength(200);

        builder.Property(x => x.ResolvedBy)
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.AiSummary)
            .HasMaxLength(2000);

        // ReadingsJson stored as nvarchar(max) — 3 reading objects is small
        builder.Property(x => x.ReadingsJson)
            .HasColumnType("nvarchar(max)");

        // Indexes for alert list page queries
        builder.HasIndex(x => new { x.TenantId, x.StatusCode, x.TriggeredAt })
            .HasDatabaseName("IX_Alerts_Tenant_Status_Triggered");

        builder.HasIndex(x => new { x.TenantId, x.SeverityCode, x.StatusCode })
            .HasDatabaseName("IX_Alerts_Tenant_Severity_Status");

        builder.HasIndex(x => x.DeviceId)
            .HasDatabaseName("IX_Alerts_DeviceId");

        // Device cascade delete: if device deleted, delete its alerts
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Threshold: NoAction — keep alerts even if threshold is removed
        builder.HasOne(x => x.Threshold)
            .WithMany()
            .HasForeignKey(x => x.AlertThresholdId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
